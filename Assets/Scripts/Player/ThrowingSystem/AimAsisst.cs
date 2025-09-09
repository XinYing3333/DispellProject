// AimAssist.cs
using UnityEngine;
using System.Collections.Generic;

public class AimAssist : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;      // 通常是主攝影機
    public Transform throwOrigin;          // 投擲發射點
    [Tooltip("可選：若提供，會以player forward作為主視角方向備援")]
    public Transform playerForwardRef;

    [Header("Detection")]
    public LayerMask interactionMask;       // 設定你的可射擊圖層
    public float detectRadius = 6f;        // 偵測半徑
    [Range(1f, 90f)] public float maxSnapAngle = 25f; // 與前向的最大夾角
    public float maxDistance = 30f;        // 最遠距離
    public int maxTargetsCheck = 24;       // OverlapSphereNonAlloc 上限

    [Header("Stickiness")]
    public float targetStickyTime = 0.2f;  // 鎖定後最少持續時間（避免抖動）
    public float switchThreshold = 0.15f;  // 新目標分數需比當前高出多少才切換

    [Header("LOS (可見性)")]
    public bool requireLineOfSight = true;
    public LayerMask obstacleMask;         // 場景遮擋用

    [Header("Debug")]
    public bool drawGizmos = true;

    private readonly Collider[] _hits = new Collider[128];
    private Targetable _current;
    private float _lockTimer;

    public Targetable CurrentTarget => _current;

    void Reset()
    {
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        var best = FindBestTarget(out float bestScore);

        if (best != _current)
        {
            // 關掉舊高亮（Aim 通道）
            if (_current)
            {
                // 如果你已有 SetAimHighlight，建議用下面這行
                _current.SetHighlighted(false);      // ← 若你有 SetAimHighlight：_current.SetAimHighlight(false);
            }

            _current = best;
            _lockTimer = targetStickyTime;

            // 開啟新高亮（Aim 通道）
            if (_current)
            {
                _current.SetHighlighted(true);       // ← 若你有 SetAimHighlight：_current.SetAimHighlight(true);
            }
        }
        /*if (_current && _lockTimer > 0f)
        {
            _lockTimer -= Time.deltaTime;

            // 若新目標分數沒有明顯更好，避免立刻切換
            if (best && best != _current)
            {
                var currScore = ScoreTarget(_current);
                if (bestScore < currScore * (1f + switchThreshold))
                    best = _current;
            }
        }

        if (best != _current)
        {
            // 關掉舊高亮
            if (_current) _current.SetHighlighted(false);

            _current = best;
            _lockTimer = targetStickyTime;

            // 開啟新高亮
            if (_current) _current.SetHighlighted(true);
        }*/
    }

    Targetable FindBestTarget(out float bestScore)
    {
        bestScore = float.MinValue;
        Targetable best = null;

        Vector3 origin = cameraTransform ? cameraTransform.position : (playerForwardRef ? playerForwardRef.position : transform.position);
        Vector3 forward = cameraTransform ? cameraTransform.forward : (playerForwardRef ? playerForwardRef.forward : transform.forward);

        int count = Physics.OverlapSphereNonAlloc(
            origin,
            detectRadius,
            _hits,
            interactionMask,                         // ← 替換舊的 shootableMask
            QueryTriggerInteraction.Ignore
        );
        for (int i = 0; i < count && i < maxTargetsCheck; i++)
        {
            var c = _hits[i];
            if (!c) continue;

            var t = c.GetComponentInParent<Targetable>(); // ← 僅收 Targetable
            if (!t) continue;

            // LOS 可見性
            if (requireLineOfSight)
            {
                if (Physics.Linecast(origin, t.GetAimPoint(), out RaycastHit hit, obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    // 擋到的不是目標
                    if (!hit.collider.transform.IsChildOf(t.transform)) continue;
                }
            }

            float s = ScoreTarget(t); // 分數越高越好
            if (s > bestScore)
            {
                bestScore = s;
                best = t;
            }
        }

        return best;
    }

    float ScoreTarget(Targetable t)
    {
        if (!t) return float.MinValue;
        Vector3 origin = cameraTransform ? cameraTransform.position : (playerForwardRef ? playerForwardRef.position : transform.position);
        Vector3 forward = cameraTransform ? cameraTransform.forward : (playerForwardRef ? playerForwardRef.forward : transform.forward);

        Vector3 to = t.GetAimPoint() - origin;
        float dist = to.magnitude;
        float angle = Vector3.Angle(forward, to);

        // 距離越近/角度越小分數越高；你可以依手感再調整權重
        float distScore = Mathf.InverseLerp(maxDistance, 0f, dist); // 0~1
        float angleScore = Mathf.InverseLerp(maxSnapAngle, 0f, angle);

        return distScore * 0.5f + angleScore * 0.5f;
    }

    /// 供投擲系統取方向：若有目標，回傳目標方向；否則回傳視角 forward。
    public Vector3 GetAimDirection()
    {
        Vector3 origin = throwOrigin ? throwOrigin.position :
                         (cameraTransform ? cameraTransform.position : transform.position);

        if (_current)
        {
            Vector3 aim = _current.GetAimPoint() - origin;
            return aim.sqrMagnitude > 0.0001f ? aim.normalized : (cameraTransform ? cameraTransform.forward : transform.forward);
        }

        return cameraTransform ? cameraTransform.forward :
               (playerForwardRef ? playerForwardRef.forward : transform.forward);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Vector3 origin = cameraTransform ? cameraTransform.position : transform.position;
        Vector3 forward = cameraTransform ? cameraTransform.forward : transform.forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, detectRadius);

        // 可視化「夾角錐」
        Gizmos.color = Color.cyan;
        var rot1 = Quaternion.AngleAxis(+maxSnapAngle, Vector3.up);
        var rot2 = Quaternion.AngleAxis(-maxSnapAngle, Vector3.up);
        Gizmos.DrawRay(origin, rot1 * forward * detectRadius);
        Gizmos.DrawRay(origin, rot2 * forward * detectRadius);

        if (_current)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, _current.GetAimPoint());
        }
    }
}
