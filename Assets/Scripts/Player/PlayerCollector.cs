// PlayerCollector.cs
using UnityEngine;

public class PlayerCollector : MonoBehaviour
{
    [Header("Detect (前方錐形)")]
    public Transform center;            // 通常是玩家的 collectPoint/胸口
    public Transform forwardRef;        // 通常用相機或玩家 forward
    public LayerMask interactionMask;
    public float radius = 2.0f;
    [Range(1f,180f)] public float angle = 90f;
    
    // ---- 放在 PlayerCollector 欄位區 ----
    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0f, 0.85f, 1f, 0.35f); // 青藍半透明
    [SerializeField] private float gizmoRayLen = 2f; // 錐角兩側射線長度（視覺參考）

    private readonly Collider[] _hits = new Collider[64];
    private float _cosHalf;

    // 與控制器溝通（由上層注入）
    private System.Func<bool> _isBusy;
    private System.Action<Rigidbody, bool> _onPulledResult;

    void OnValidate() => _cosHalf = Mathf.Cos(Mathf.Deg2Rad * (angle * 0.5f));
    void Awake()      => _cosHalf = Mathf.Cos(Mathf.Deg2Rad * (angle * 0.5f));

    public void SetBusyChecker(System.Func<bool> f) => _isBusy = f;
    public void SetOnPulledResult(System.Action<Rigidbody, bool> cb) => _onPulledResult = cb;

    // 最小版核心：**立即**處理最近的一個（不做拉動/吸附動畫）
    public void TryAbsorbOnce()
    {
        if (!center) return;
        if (_isBusy != null && _isBusy()) return;

        int n = Physics.OverlapSphereNonAlloc(center.position, radius, _hits, interactionMask, QueryTriggerInteraction.Ignore);

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
        //var collect = bestT.GetComponentInParent<ICollectable>();
        /*if (collect != null)
        {
            collect.Collect();
            _onPulledResult?.Invoke(null, true); // 已收進背包
            return;
        }*/

        var rb = bestT.GetComponentInParent<Rigidbody>();
        if (rb)
        {
            _onPulledResult?.Invoke(rb, false);  // 交給上層放手上
        }
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
        Vector3 leftDir  = Quaternion.AngleAxis(-half, Vector3.up) * forward;
        Vector3 rightDir = Quaternion.AngleAxis(+half, Vector3.up) * forward;
        Gizmos.DrawRay(origin, leftDir  * gizmoRayLen);
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
