// PlayerCollector.cs

using System.Collections.Generic;
using Player.InteractionSystem;
using UnityEngine;
using System.Linq;
using DG.Tweening;

/// <summary>
/// 吸收功能脚本：提供【吸收】和【通知收集】的公開方法。
/// </summary>

public class PlayerCollector : MonoBehaviour
{
    [Header("Detect (前方錐形)")] public Transform center; // 通常是玩家的 collectPoint/胸口
    public Transform forwardRef; // 通常用相機或玩家 forward
    public LayerMask interactionMask;
    public float radius = 2.0f;
    [Range(1f, 180f)] public float angle = 90f;

    [Header("Absorb Tween")] [SerializeField, Tooltip("物件被吸到玩家面前的時間（秒）")]
    private float pullDuration = 0.35f;

    [SerializeField, Tooltip("吸到玩家面前的距離（沿著 forward）")]
    private float frontOffset = 0.6f;

    [SerializeField, Tooltip("吸的 Ease 曲線")]
    private Ease pullEase = Ease.Linear;

    [SerializeField, Tooltip("拉動時是否暫時關閉碰撞與重力")]
    private bool disablePhysicsDuringPull = true;


    // ---- 放在 PlayerCollector 欄位區 ----
    [Header("Gizmos")] [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0f, 0.85f, 1f, 0.35f); // 青藍半透明
    [SerializeField] private float gizmoRayLen = 2f; // 錐角兩側射線長度（視覺參考）

    private readonly Collider[] _hits = new Collider[64];
    private float _cosHalf;

    // 與控制器溝通（由上層注入）
    private System.Func<bool> _isBusy;
    private System.Action<Rigidbody, bool> _onPulledResult;

    void OnValidate() => _cosHalf = Mathf.Cos(Mathf.Deg2Rad * (angle * 0.5f));
    void Awake() => _cosHalf = Mathf.Cos(Mathf.Deg2Rad * (angle * 0.5f));

    public void SetBusyChecker(System.Func<bool> f) => _isBusy = f;
    public void SetOnPulledResult(System.Action<Rigidbody, bool> cb) => _onPulledResult = cb;

    // 追蹤目前正在被拉動的目標，避免重複處理
    private readonly HashSet<Transform> _pulling = new HashSet<Transform>();
    private int _activePulls = 0; // 作為 isBusy 的其中一個因素

    private bool IsCollectorBusy()
    {
        // 原本上層注入的 busy + 自己還在拉東西
        bool upperBusy = _isBusy != null && _isBusy();
        return upperBusy || _activePulls > 0;
    }

    private Vector3 GetFrontPoint()
    {
        Vector3 fwd = forwardRef ? forwardRef.forward : transform.forward;
        return center.position + fwd * frontOffset;
    }

    private void PrepareForPull(Transform t, out Rigidbody rb, out Collider[] cols)
    {
        rb = t.GetComponentInParent<Rigidbody>();
        cols = t.GetComponentsInChildren<Collider>(true);

        if (!disablePhysicsDuringPull) return;

        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            //rb.angularVelocity = Vector3.zero;
        }

        foreach (var c in cols) c.enabled = false;
    }

    private void RestoreAfterPull(Transform t, Rigidbody rb, Collider[] cols)
    {
        if (!disablePhysicsDuringPull) return;

        // 物件可能在 Collect() 內被銷毀
        if (t == null) return;

        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        foreach (var c in cols)
        {
            if (c) c.enabled = true;
        }
    }


    // 最小版核心：**立即**處理最近的一個（不做拉動/吸附動畫）
    public void TryAbsorbOnce()
    {
        if (!center) return;
        if (IsCollectorBusy()) return;

        int n = Physics.OverlapSphereNonAlloc(center.position, radius, _hits, interactionMask,
            QueryTriggerInteraction.Ignore);

        Transform bestT = null;
        float bestScore = float.MinValue;

        Vector3 fwd = forwardRef ? forwardRef.forward : transform.forward;

        for (int i = 0; i < n; i++)
        {
            var c = _hits[i];
            if (!c) continue;

            Vector3 to = c.transform.position - center.position;
            float d2 = to.sqrMagnitude;
            if (d2 < 1e-6f) continue;
            float invd = 1f / Mathf.Sqrt(d2);
            Vector3 toN = to * invd;

            if (Vector3.Dot(fwd, toN) < _cosHalf) continue;

            float dist = 1f / Mathf.Max(0.0001f, Mathf.Sqrt(d2));
            float score = dist; // 越近越好（先簡單）

            if (score > bestScore)
            {
                bestScore = score;
                bestT = c.transform;
            }
        }

        if (!bestT) return;

        // 語意決定：可收集 → Collect；否則若有 Rigidbody → 交給上層放手上
        var collect = bestT.GetComponentInParent<ICollectable>();
        if (collect != null)
        {
            // 拉到面前 → 到位後才 Collect
            StartPullThenCollect(bestT, collect);
            return;
        }

        var rb = bestT.GetComponentInParent<Rigidbody>();
        if (rb)
        {
            _onPulledResult?.Invoke(rb, false); // 交給上層放手上
        }
    }

    private void StartPullThenCollect(Transform target, ICollectable collectable)
    {
        if (!target || _pulling.Contains(target)) return;
        _pulling.Add(target);
        _activePulls++;

        // 準備：關掉物理/碰撞（可選）
        PrepareForPull(target, out var rb, out var cols);

        // 計算目標點
        Vector3 end = GetFrontPoint();

        // 確保有可 tween 的 transform（直接對 target 做）
        // 這裡用世界座標直線移動；若你想要微微弧線可以改用 Sequence + DOMove + DOJump
        var tween = target.DOMove(end, pullDuration)
            .SetEase(pullEase)
            .OnComplete(() =>
            {
                try
                {
                    // 到位後才執行收集
                    collectable.Collect();
                    _onPulledResult?.Invoke(null, true);
                }
                finally
                {
                    // 邏輯上 Collect 可能把目標刪掉了，再做還原前先檢查
                    RestoreAfterPull(target, rb, cols);
                    _pulling.Remove(target);
                    _activePulls = Mathf.Max(0, _activePulls - 1);
                }
            })
            .OnKill(() =>
            {
                // 若 tween 被外部中止，也要回收狀態
                RestoreAfterPull(target, rb, cols);
                _pulling.Remove(target);
                _activePulls = Mathf.Max(0, _activePulls - 1);
            });

        // 保險：若物件在期間被銷毀，Tween 自己會 Kill → OnKill 會收攤
    }


    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !center) return;

        // 半徑球
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(center.position, radius);

        // 前方錐形（水平角度）
        Vector3 origin = center.position;
        Vector3 forward = (forwardRef ? forwardRef.forward : transform.forward);
        float half = angle * 0.5f;

        // 畫兩條邊界射線（水平面）
        Gizmos.color = Color.cyan;
        Vector3 leftDir = Quaternion.AngleAxis(-half, Vector3.up) * forward;
        Vector3 rightDir = Quaternion.AngleAxis(+half, Vector3.up) * forward;
        Gizmos.DrawRay(origin, leftDir * gizmoRayLen);
        Gizmos.DrawRay(origin, rightDir * gizmoRayLen);

        // 畫前向參考線
        Gizmos.color = Color.green;
        Gizmos.DrawRay(origin, forward * gizmoRayLen);

        // 也給錐形一個扇面近似（選擇性）
        int segments = 24;
        float step = angle / segments;
        Vector3 prev = origin + (Quaternion.AngleAxis(-half, Vector3.up) * forward) * gizmoRayLen;
        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        for (int i = 1; i <= segments; i++)
        {
            float a = -half + step * i;
            Vector3 next = origin + (Quaternion.AngleAxis(a, Vector3.up) * forward) * gizmoRayLen;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}