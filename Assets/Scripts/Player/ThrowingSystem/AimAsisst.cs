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
    public Color detectColor = new Color(1f, 0.85f, 0f, 0.25f);
    public Color fovColor = Color.yellow;
    public Color forwardColor = Color.white;
    public Color targetLineColor = Color.green;

    private readonly Collider[] _hits = new Collider[128];
    private Targetable _current;

    public bool Scanning { get; private set; } = false;
    
    public Targetable CurrentTarget => _current;

    void Reset()
    {
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (!Scanning) return;
        var next = FindBestTarget();

        if (!ReferenceEquals(next, _current))
        {
            if (_current) _current.SetAimActive(false);
            _current = next;
            if (_current) _current.SetAimActive(true);
        }
    }

    void OnDisable()
    {
        if (_current) _current.SetAimActive(false);
        _current = null;
        Scanning = false;
    }

    public void SetScanning(bool on)
    {
        if (Scanning == on) return;
        Scanning = on;
        if (!on)
        {
            if (_current) _current.SetAimActive(false);
            _current = null;
        }
    }

    /// <summary>
    /// 供外部獲取當前鎖定目標的 Transform
    /// </summary>
    public Transform GetTarget()
    {
        return _current != null ? _current.transform : null;
    }
    
    public Vector3 GetAimDirection()
    {
        if (!_current)
            return cameraTransform ? cameraTransform.forward : transform.forward;

        var origin = throwOrigin ? throwOrigin.position :
            (cameraTransform ? cameraTransform.position : transform.position);

        var aimPoint = GetTargetCenter(_current);
        var dir = aimPoint - origin;

        return dir.sqrMagnitude > 0.0001f
            ? dir.normalized
            : (cameraTransform ? cameraTransform.forward : transform.forward);
    }

    private Targetable FindBestTarget()
    {
        var origin = cameraTransform ? cameraTransform.position : transform.position;
        var forward = cameraTransform ? cameraTransform.forward : transform.forward;
        float bestScore = float.MinValue;
        Targetable best = null;

        int n = Physics.OverlapSphereNonAlloc(origin, detectRadius, _hits, interactionMask, QueryTriggerInteraction.Ignore);
        int limit = Mathf.Min(n, Mathf.Min(maxHits, _hits.Length));

        for (int i = 0; i < limit; i++)
        {
            var c = _hits[i];
            if (!c) continue;
            var t = c.GetComponentInParent<Targetable>();
            if (!t) continue;

            var to = GetTargetCenter(t) - origin;
            float dist = to.magnitude;
            if (dist <= 0.0001f) continue;

            float ang = Vector3.Angle(forward, to);
            if (ang > maxAngle) continue;

            float score = Mathf.InverseLerp(detectRadius, 0f, dist) * 0.5f +
                          Mathf.InverseLerp(maxAngle, 0f, ang) * 0.5f;

            if (score > bestScore) { bestScore = score; best = t; }
        }
        return best;
    }
    
    private Vector3 GetTargetCenter(Targetable t)
    {
        var col = t.GetComponentInChildren<Collider>();
        if (col)
            return col.bounds.center;

        var rend = t.GetComponentInChildren<Renderer>();
        if (rend)
            return rend.bounds.center;

        return t.GetAimPoint();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Vector3 origin = cameraTransform ? cameraTransform.position : transform.position;
        Vector3 forward = cameraTransform ? cameraTransform.forward : transform.forward;

        Gizmos.color = detectColor;
        Gizmos.DrawWireSphere(origin, detectRadius);

        Gizmos.color = forwardColor;
        Gizmos.DrawRay(origin, forward * Mathf.Min(2f, detectRadius));

        Gizmos.color = fovColor;
        var rotL = Quaternion.AngleAxis(-maxAngle, Vector3.up);
        var rotR = Quaternion.AngleAxis(+maxAngle, Vector3.up);
        Gizmos.DrawRay(origin, rotL * forward * Mathf.Min(2f, detectRadius));
        Gizmos.DrawRay(origin, rotR * forward * Mathf.Min(2f, detectRadius));

        if (CurrentTarget)
        {
            Gizmos.color = targetLineColor;
            Gizmos.DrawLine(origin, GetTargetCenter(CurrentTarget));
        }
    }
}