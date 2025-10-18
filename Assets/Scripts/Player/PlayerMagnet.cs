using System.Collections.Generic;
using UnityEngine;

public class PlayerMagnet : MonoBehaviour
{
    public float radius = 4.5f;
    public float suckStartDistance = 2.2f;
    public float flySpeed = 12f;
    public float arrivalDistance = 0.35f;

    private static readonly HashSet<OfferingPickup> _candidates = new();

    public static void Register(OfferingPickup p)   { if (p) _candidates.Add(p); }
    public static void Unregister(OfferingPickup p) { if (p) _candidates.Remove(p); }

    void LateUpdate()
    {
        if (_candidates.Count == 0) return;

        Vector3 center = transform.position;
        float r2 = radius * radius;
        float suck2 = suckStartDistance * suckStartDistance;

        // 防禦性：掃掉已失效引用
        _candidates.RemoveWhere(p => p == null || !p.isActiveAndEnabled);

        foreach (var p in _candidates)
        {
            if (p.Collected) continue;

            Vector3 to = center - p.transform.position;
            float d2 = to.sqrMagnitude;
            if (d2 > r2) continue;

            float t = Mathf.InverseLerp(r2, 0f, d2); // 遠→近 0..1
            float spd = Mathf.Lerp(flySpeed * 0.7f, flySpeed * 1.6f, t);
            bool strong = d2 <= suck2;

            p.AttractTo(center, spd, strong, arrivalDistance);
        }
    }
}
