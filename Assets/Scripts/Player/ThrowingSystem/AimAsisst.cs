// AimAssist.cs
using UnityEngine;

public class AimAssist : MonoBehaviour
{
    [Header("Refs")]
    public Transform cameraTransform;
    public Transform throwOrigin;

    [Header("Detect")]
    public LayerMask interactionMask;
    public float detectRadius = 8f;
    [Range(1f, 90f)] public float maxAngle = 30f;
    public int maxHits = 64;

    [Header("Gizmos")]
    public bool drawGizmos = true;
    public Color detectColor = new Color(1f, 0.85f, 0f, 0.25f); // 金黃半透明
    public Color fovColor = Color.yellow;
    public Color forwardColor = Color.white;
    public Color targetLineColor = Color.green;
    
    private readonly Collider[] _hits = new Collider[128];
    private Targetable _current;

    public Targetable CurrentTarget => _current;

    void Reset()
    {
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        _current = FindBestTarget();
        if(_current)_current.SetHighlighted(true);
    }
    
    public Vector3 GetAimDirection()
    {
        if (!_current)
            return cameraTransform ? cameraTransform.forward : transform.forward;

        var origin = throwOrigin ? throwOrigin.position :
                     (cameraTransform ? cameraTransform.position : transform.position);
        var dir = (_current.GetAimPoint() - origin);
        return dir.sqrMagnitude > 0.0001f ? dir.normalized :
               (cameraTransform ? cameraTransform.forward : transform.forward);
    }

    private Targetable FindBestTarget()
    {
        var origin = cameraTransform ? cameraTransform.position : transform.position;
        var forward = cameraTransform ? cameraTransform.forward : transform.forward;
        float bestScore = float.MinValue;
        Targetable best = null;

        int n = Physics.OverlapSphereNonAlloc(origin, detectRadius, _hits, interactionMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n && i < maxHits; i++)
        {
            var c = _hits[i];
            if (!c) continue;
            var t = c.GetComponentInParent<Targetable>();
            if (!t) continue;

            var to = t.GetAimPoint() - origin;
            float dist = to.magnitude;
            if (dist <= 0.0001f) continue;

            float angle = Vector3.Angle(forward, to);
            if (angle > maxAngle) continue;

            float score = Mathf.InverseLerp(detectRadius, 0f, dist) * 0.5f +
                          Mathf.InverseLerp(maxAngle, 0f, angle) * 0.5f;

            if (score > bestScore) { bestScore = score; best = t; }
        }
        return best;
    }
    
    // ---- 放在 AimAssist 類別結尾 ----
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Vector3 origin = cameraTransform ? cameraTransform.position : transform.position;
        Vector3 forward = cameraTransform ? cameraTransform.forward : transform.forward;

        // 偵測半徑
        Gizmos.color = detectColor;
        Gizmos.DrawWireSphere(origin, detectRadius);

        // 前向參考
        Gizmos.color = forwardColor;
        Gizmos.DrawRay(origin, forward * Mathf.Min(2f, detectRadius));

        // 視野夾角兩邊
        Gizmos.color = fovColor;
        var rotL = Quaternion.AngleAxis(-maxAngle, Vector3.up);
        var rotR = Quaternion.AngleAxis(+maxAngle, Vector3.up);
        Gizmos.DrawRay(origin, rotL * forward * Mathf.Min(2f, detectRadius));
        Gizmos.DrawRay(origin, rotR * forward * Mathf.Min(2f, detectRadius));

        // 目前目標的指向線
        if (CurrentTarget)
        {
            Gizmos.color = targetLineColor;
            Gizmos.DrawLine(origin, CurrentTarget.GetAimPoint());
        }
    }
}
