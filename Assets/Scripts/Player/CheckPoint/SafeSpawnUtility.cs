// SafeSpawnUtility.cs
using UnityEngine;

public static class SafeSpawnUtility
{
    /// <summary>
    /// 修正重生位置：貼地、避開過陡斜坡、遠離邊緣。
    /// </summary>
    public static bool EnsureSafeSpawn(
        Vector3 desiredPos,
        Quaternion desiredRot,
        out Vector3 finalPos,
        out Quaternion finalRot,
        LayerMask groundMask,
        float rayDown = 6f,
        float slopeLimitDeg = 45f,
        float probeRadius = 0.35f,
        float edgeProbeDist = 0.7f,
        float safeInset = 2f,
        int radialChecks = 12)
    {
        finalPos = desiredPos;
        finalRot = desiredRot;

        // 1️⃣ 向下貼地
        if (!Physics.Raycast(desiredPos + Vector3.up, Vector3.down, out var hit, rayDown + 1f, groundMask, QueryTriggerInteraction.Ignore))
            return false;

        Vector3 pos = hit.point;
        Vector3 nrm = hit.normal;
        float slopeDeg = Vector3.Angle(nrm, Vector3.up);

        // 2️⃣ 若太陡 → 在附近搜尋較平的位置
        if (slopeDeg > slopeLimitDeg)
        {
            if (!FindFlatterNearby(pos, groundMask, slopeLimitDeg, probeRadius, out pos, out nrm))
            {
                // 找不到平坦點 → 用原命中點
            }
        }

        // 3️⃣ 邊緣檢查：周圍若有懸空，往內縮
        Vector3 inward = Vector3.zero;
        for (int i = 0; i < radialChecks; i++)
        {
            float ang = (Mathf.PI * 2f) * i / radialChecks;
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
            Vector3 p   = pos + dir * edgeProbeDist + Vector3.up * 0.2f;

            if (!Physics.Raycast(p, Vector3.down, out var hit2, 0.6f, groundMask, QueryTriggerInteraction.Ignore))
            {
                // 探不到地 → 視為邊緣外側，把相反方向累加成「內縮方向」
                inward += -dir;
            }
        }

        if (inward.sqrMagnitude > 0.0001f)
        {
            inward = Vector3.ProjectOnPlane(inward.normalized, nrm);
            pos += inward * safeInset;

            // 再貼一次地面
            if (Physics.Raycast(pos + Vector3.up, Vector3.down, out var hit3, rayDown + 1f, groundMask, QueryTriggerInteraction.Ignore))
            {
                pos = hit3.point;
                nrm = hit3.normal;
            }
        }

        // 4️⃣ 修正朝向，使角色面向沿地面切線方向
        Vector3 forward = Vector3.ProjectOnPlane(desiredRot * Vector3.forward, nrm).normalized;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.ProjectOnPlane(Vector3.forward, nrm).normalized;

        finalPos = pos;
        finalRot = Quaternion.LookRotation(forward, nrm);
        return true;
    }

    private static bool FindFlatterNearby(
        Vector3 center,
        LayerMask groundMask,
        float slopeLimitDeg,
        float probeRadius,
        out Vector3 bestPos,
        out Vector3 bestNormal)
    {
        bestPos = center; bestNormal = Vector3.up;
        float bestSlope = 999f;
        bool found = false;

        Vector3[] offsets = {
            Vector3.zero,
            new(probeRadius,0,0),
            new(-probeRadius,0,0),
            new(0,0,probeRadius),
            new(0,0,-probeRadius),
            new(probeRadius,0,probeRadius),
            new(probeRadius,0,-probeRadius),
            new(-probeRadius,0,probeRadius),
            new(-probeRadius,0,-probeRadius),
        };

        foreach (var o in offsets)
        {
            Vector3 p = center + o + Vector3.up;
            if (Physics.Raycast(p, Vector3.down, out var h, 2f + probeRadius, groundMask, QueryTriggerInteraction.Ignore))
            {
                float slope = Vector3.Angle(h.normal, Vector3.up);
                if (slope < bestSlope)
                {
                    bestSlope = slope;
                    bestPos = h.point;
                    bestNormal = h.normal;
                    if (bestSlope <= slopeLimitDeg) found = true;
                }
            }
        }

        return found;
    }
}
