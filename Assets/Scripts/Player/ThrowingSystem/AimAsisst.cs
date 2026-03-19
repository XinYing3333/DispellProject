using UnityEngine;

public class AimAssist : MonoBehaviour
{
    public Transform cameraTransform;
    public Transform throwOrigin;
    public LayerMask interactionMask;
    public float detectRadius = 8f;
    [Range(1f, 90f)] public float maxAngle = 30f;

    // 關鍵修正：確保這些是 public，否則 ThrowingSystem 會報錯
    public Targetable CurrentTarget { get; private set; }
    public TargetState assistMode = TargetState.SpellReady;
    public bool Scanning { get; private set; }

    private readonly Collider[] _hits = new Collider[64];

    void Update()
    {
        if (!Scanning) return;
        var next = FindBestTarget();

        if (next != CurrentTarget)
        {
            if (CurrentTarget) CurrentTarget.SetTargetState(TargetState.None);
            CurrentTarget = next;
            if (CurrentTarget) CurrentTarget.SetTargetState(assistMode);
        }
    }

    public void SetScanning(bool on)
    {
        Scanning = on;
        if (!on && CurrentTarget)
        {
            CurrentTarget.SetTargetState(TargetState.None);
            CurrentTarget = null;
        }
    }

    public void SetAssistMode(TargetState mode)
    {
        if (assistMode == mode) return;
        if (CurrentTarget) CurrentTarget.SetTargetState(TargetState.None);
        CurrentTarget = null;
        assistMode = mode;
    }

    private Targetable FindBestTarget()
    {
        Vector3 origin = cameraTransform ? cameraTransform.position : transform.position;
        Vector3 forward = cameraTransform ? cameraTransform.forward : transform.forward;
        Targetable best = null;
        float bestScore = -1f;

        int n = Physics.OverlapSphereNonAlloc(origin, detectRadius, _hits, interactionMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var t = _hits[i].GetComponentInParent<Targetable>();
            if (!t) continue;

            Vector3 to = t.GetAimPoint() - origin;
            float ang = Vector3.Angle(forward, to);
            if (ang > maxAngle) continue;

            float score = (1f - (ang / maxAngle)) + (1f - (to.magnitude / detectRadius));
            if (score > bestScore) { bestScore = score; best = t; }
        }
        return best;
    }

    public Vector3 GetAimDirection() => cameraTransform ? cameraTransform.forward : transform.forward;
}