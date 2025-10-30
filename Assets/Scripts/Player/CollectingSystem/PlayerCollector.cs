using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Player.InteractionSystem;

public class PlayerCollector : MonoBehaviour
{
    [Header("Detect (前方錐形)")]
    public Transform center;
    public Transform forwardRef;
    public LayerMask interactionMask;
    public float radius = 2.0f;
    [Range(1f, 180f)] public float angle = 90f;

    [Header("Absorb Tween")]
    [SerializeField] private float pullDuration = 0.35f;
    [SerializeField] private float frontOffset  = 0.6f;
    [SerializeField] private Ease  pullEase     = Ease.Linear;

    [Header("Collect Flow")]
    [SerializeField] private bool disablePhysicsDuringPull = true;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gizmoColor = new(0f, 0.85f, 1f, 0.35f);
    [SerializeField] private float gizmoRayLen = 2f;

    private readonly Collider[] _hits = new Collider[64];
    private float _cosHalf;

    private System.Func<bool> _isBusy;
    private System.Action<Rigidbody, bool> _onPulledResult;

    // ===== 狀態 =====
    // 原本就有：
    private readonly HashSet<Transform> _pulling = new();
    private int _activePulls = 0;

    // 👉 新增：記錄正在拉的 tween、以及被關掉的碰撞器
    private readonly Dictionary<Transform, Tween> _pullTweens = new();
    private readonly Dictionary<Transform, (Rigidbody rb, Collider[] cols)> _pulledPhysics = new();

    void OnValidate() => _cosHalf = Mathf.Cos(Mathf.Deg2Rad * (angle * 0.5f));
    void Awake()      => _cosHalf = Mathf.Cos(Mathf.Deg2Rad * (angle * 0.5f));

    public void SetBusyChecker(System.Func<bool> f)             => _isBusy = f;
    public void SetOnPulledResult(System.Action<Rigidbody,bool> cb) => _onPulledResult = cb;

    private bool IsCollectorBusy() => (_isBusy != null && _isBusy()) || _activePulls > 0;

    private Vector3 GetFrontPoint()
    {
        Vector3 fwd = forwardRef ? forwardRef.forward : transform.forward;
        return center ? center.position + fwd * frontOffset : transform.position + fwd * frontOffset;
    }

    public void TryAbsorbOnce()
    {
        if (!center || IsCollectorBusy()) return;

        int n = Physics.OverlapSphereNonAlloc(center.position, radius, _hits, interactionMask, QueryTriggerInteraction.Ignore);

        Transform bestT = null;
        float bestScore = float.MinValue;

        Vector3 fwd = forwardRef ? forwardRef.forward : transform.forward;
        float cosHalf = _cosHalf;

        for (int i = 0; i < n; i++)
        {
            var c = _hits[i];
            if (!c) continue;

            Vector3 to  = c.transform.position - center.position;
            float d2    = to.sqrMagnitude;
            if (d2 < 1e-6f) continue;

            Vector3 toN = to / Mathf.Sqrt(d2);
            if (Vector3.Dot(fwd, toN) < cosHalf) continue;

            float score = 1f / Mathf.Max(0.0001f, Mathf.Sqrt(d2));
            if (score > bestScore) { bestScore = score; bestT = c.transform; }
        }

        if (!bestT) return;

        var collect = bestT.GetComponentInParent<ICollectable>();
        if (collect != null)
        {
            StartPullThenCollect(bestT, collect);
            return;
        }

        var rb = bestT.GetComponentInParent<Rigidbody>();
        if (rb)
        {
            StartPullThenHandOff(bestT, rb);
        }
    }

    // =============== Collect 版 ===============
    private void StartPullThenCollect(Transform target, ICollectable collectable)
    {
        if (!target || _pulling.Contains(target)) return;
        _pulling.Add(target);
        _activePulls++;

        PrepareForPull(target, out var rb, out var cols);

        Vector3 end = GetFrontPoint();

        var tw = target.DOMove(end, pullDuration)
            .SetEase(pullEase)
            .OnComplete(() =>
            {
                // 有可能在中途被取消，這裡要再確認還在 pulling
                try
                {
                    collectable.Collect();
                    _onPulledResult?.Invoke(null, true);
                }
                finally
                {
                    RestoreAfterPull(target, rb, cols);
                    CleanupPullState(target);
                }
            })
            .OnKill(() =>
            {
                // 被外部 CancelAllPulls() 殺掉也會進來這裡
                RestoreAfterPull(target, rb, cols);
                CleanupPullState(target);
            });

        _pullTweens[target] = tw;
        if (disablePhysicsDuringPull)
            _pulledPhysics[target] = (rb, cols);
    }

    // =============== 非 Collect 版 ===============
    private void StartPullThenHandOff(Transform target, Rigidbody rb)
    {
        if (!target || _pulling.Contains(target)) return;
        _pulling.Add(target);
        _activePulls++;

        Vector3 endPos = GetFrontPoint();

        var origCCD    = rb.collisionDetectionMode;
        var origInterp = rb.interpolation;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;

        var tw = rb.DOMove(endPos, pullDuration)
            .SetEase(pullEase)
            .SetUpdate(UpdateType.Fixed)
            .OnComplete(() =>
            {
                try
                {
                    _onPulledResult?.Invoke(rb, false);
                }
                finally
                {
                    rb.collisionDetectionMode = origCCD;
                    rb.interpolation          = origInterp;
                    CleanupPullState(target);
                }
            })
            .OnKill(() =>
            {
                // 被 cancel 的時候要還原物理
                rb.collisionDetectionMode = origCCD;
                rb.interpolation          = origInterp;
                CleanupPullState(target);
            });

        _pullTweens[target] = tw;
    }

    // =============== 取消全部拉取（給 InteractionController 用） ===============
    public void CancelAllPulls()
    {
        // 1) 先 Kill 所有 tween
        foreach (var kv in _pullTweens)
        {
            var t = kv.Value;
            if (t.IsActive()) t.Kill(false);
        }
        _pullTweens.Clear();

        // 2) 把被我們關掉物理的還原
        foreach (var kv in _pulledPhysics)
        {
            var target = kv.Key;
            var (rb, cols) = kv.Value;
            RestoreAfterPull(target, rb, cols);
        }
        _pulledPhysics.Clear();

        // 3) 狀態清空
        _pulling.Clear();
        _activePulls = 0;
    }

    // =============== 小工具 ===============
    private void CleanupPullState(Transform target)
    {
        _pulling.Remove(target);
        _activePulls = Mathf.Max(0, _activePulls - 1);
        _pullTweens.Remove(target);
        _pulledPhysics.Remove(target);
    }

    private void PrepareForPull(Transform t, out Rigidbody rb, out Collider[] cols)
    {
        rb   = t.GetComponentInParent<Rigidbody>();
        cols = t.GetComponentsInChildren<Collider>(true);

        if (!disablePhysicsDuringPull) return;
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
        }
        foreach (var c in cols) c.enabled = false;
    }

    private void RestoreAfterPull(Transform t, Rigidbody rb, Collider[] cols)
    {
        if (!disablePhysicsDuringPull) return;
        if (!t) return;

        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity  = true;
        }
        if (cols != null)
        {
            foreach (var c in cols)
            {
                if (c) c.enabled = true;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !center) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(center.position, radius);

        Vector3 origin  = center.position;
        Vector3 forward = forwardRef ? forwardRef.forward : transform.forward;
        float half = angle * 0.5f;

        Gizmos.color = Color.cyan;
        Vector3 leftDir  = Quaternion.AngleAxis(-half, Vector3.up) * forward;
        Vector3 rightDir = Quaternion.AngleAxis(+half, Vector3.up) * forward;
        Gizmos.DrawRay(origin, leftDir  * gizmoRayLen);
        Gizmos.DrawRay(origin, rightDir * gizmoRayLen);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(origin, forward * gizmoRayLen);
    }
}
