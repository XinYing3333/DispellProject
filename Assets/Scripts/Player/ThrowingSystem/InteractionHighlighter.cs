using System.Collections.Generic;
using UnityEngine;

public class InteractionHighlighter : MonoBehaviour
{
    [Header("參考")]
    public Transform center;           // 掃描中心（建議填 Player 或你的 collectPoint）
    public Transform forwardRef;       // 角度判定參考（相機或玩家）
    public LayerMask interactionMask;  // 勾 Interactable（或 Collectible|Shootable 聯集）
    public QueryTriggerInteraction triggerQuery = QueryTriggerInteraction.Ignore;

    [Header("範圍")]
    public float radius = 3.0f;
    [Range(1f,180f)] public float angle = 120f; // 夾角（前方寬不寬）
    public float scanInterval = 0.12f;

    [Header("除外條件")]
    public bool requireVisible = false;
    public LayerMask obstacleMask;

    private readonly Collider[] _hits = new Collider[128];
    private readonly HashSet<Highlightable> _active = new();
    private float _cosHalf;

    void OnEnable()
    {
        _cosHalf = Mathf.Cos(Mathf.Deg2Rad * (angle * 0.5f));
        InvokeRepeating(nameof(Scan), Random.Range(0f, scanInterval), scanInterval);
    }

    void OnDisable()
    {
        CancelInvoke(nameof(Scan));
        // 關掉所有近距離高亮
        foreach (var h in _active) if (h) h.SetProximityHighlight(false);
        _active.Clear();
    }

    void Scan()
    {
        if (!center) return;

        int n = Physics.OverlapSphereNonAlloc(center.position, radius, _hits, interactionMask, triggerQuery);

        var newSet = new HashSet<Highlightable>();
        Vector3 fwd = (forwardRef ? forwardRef.forward : transform.forward);

        for (int i = 0; i < n; i++)
        {
            var c = _hits[i];
            if (!c) continue;

            // 角度篩選
            Vector3 to = c.transform.position - center.position;
            float d2 = to.sqrMagnitude;
            if (d2 < 1e-6f) continue;
            to /= Mathf.Sqrt(d2);
            if (Vector3.Dot(fwd, to) < _cosHalf) continue;

            // 語意篩選：有 Collectible 或 Targetable 才算「互動對象」
            var h = c.GetComponentInParent<Highlightable>();
            if (!h) continue;
            bool isInteractable = c.GetComponentInParent<Collectible>() || c.GetComponentInParent<Targetable>();
            if (!isInteractable) continue;

            // 可見性（可選）
            if (requireVisible && obstacleMask != 0)
            {
                Vector3 aimPoint = c.bounds.center;
                if (Physics.Linecast(center.position, aimPoint, out RaycastHit hit, obstacleMask, triggerQuery))
                {
                    if (!hit.collider.transform.IsChildOf(h.transform)) continue;
                }
            }

            newSet.Add(h);
        }

        // 關閉離開範圍的
        foreach (var h in _active)
            if (h && !newSet.Contains(h))
                h.SetProximityHighlight(false);

        // 開啟新進範圍的
        foreach (var h in newSet)
        {
            if (!_active.Contains(h))
                h.SetProximityHighlight(true);
        }

        _active.Clear();
        foreach (var h in newSet) _active.Add(h);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!center) return;
        Gizmos.color = new Color(0f,1f,1f,0.25f);
        Gizmos.DrawWireSphere(center.position, radius);

        Vector3 fwd = (forwardRef ? forwardRef.forward : transform.forward);
        Vector3 pos = center.position;
        float half = angle * 0.5f;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(pos, Quaternion.AngleAxis(+half, Vector3.up) * fwd * radius);
        Gizmos.DrawRay(pos, Quaternion.AngleAxis(-half, Vector3.up) * fwd * radius);
    }
#endif
}
