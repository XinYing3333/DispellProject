using UnityEngine;

public class ThrowingSystem
{
    public enum ThrowArcMode
    {
        FixedAngle,
        ToTarget
    }

    private readonly Transform throwOrigin;
    private readonly AimAssist aimAssist;
    private float throwSpeed;

    public ThrowArcMode ArcMode { get; set; } = ThrowArcMode.ToTarget;
    public float LaunchAngleDegrees { get; set; } = 35f;
    public bool PreferHighArc { get; set; } = true;
    public bool OrientToVelocity { get; set; } = true;
    public bool UseGravity { get; set; } = true;

    public ThrowingSystem(Transform throwOrigin, float throwSpeed, AimAssist aimAssist = null)
    {
        this.throwOrigin = throwOrigin;
        this.throwSpeed = throwSpeed;
        this.aimAssist = aimAssist;
    }

    public void SetThrowSpeed(float s) => throwSpeed = Mathf.Max(0f, s);

    // 核心：把「手上的 rb」直接丟出去
    public void ThrowExisting(Rigidbody rb, Transform player)
    {
        if (!rb) return;

        rb.isKinematic = false;
        rb.useGravity = UseGravity;
        rb.detectCollisions = true;

        // ★ 用實際出手位置
        Vector3 origin = rb.position;
        
        if (throwOrigin)
        {
            rb.position = throwOrigin.position;
            rb.rotation = throwOrigin.rotation;
        }
        
        Vector3 v0;

        if (ArcMode == ThrowArcMode.ToTarget && aimAssist && aimAssist.CurrentTarget)
        {
            Vector3 target = aimAssist.CurrentTarget.GetAimPoint();

            const float MaxPitch = 70f;
            bool ok = TrySolveBallisticBest(origin, target, throwSpeed, MaxPitch,
                out v0, out float usedPitchDeg, out string reason);

#if UNITY_EDITOR
            if (ok) Debug.Log($"[Throw] choose ballistic: pitch={usedPitchDeg:F1}°, speed={throwSpeed:F1}");
            else    Debug.Log($"[Throw] ballistic fallback: {reason}");
#endif
            if (!ok) v0 = FallbackFixedAngle(player); // 退回固定角
        }
        else
        {
#if UNITY_EDITOR
            Debug.Log("[Throw] no target → FixedAngle");
            
#endif
            v0 = FallbackFixedAngle(player);
        }

        rb.linearVelocity = v0;

        if (OrientToVelocity && v0.sqrMagnitude > 1e-4f)
            rb.transform.rotation = Quaternion.LookRotation(v0.normalized, Vector3.up);
#if UNITY_EDITOR
// 取樣 1 秒軌跡
        Vector3 p = origin;
        Vector3 vel = v0;
        Vector3 g = Physics.gravity;
        float step = 0.02f;
        for (float t = 0; t < 1.0f; t += step)
        {
            Vector3 pNext = origin + vel * t + 0.5f * g * (t * t);
            Debug.DrawLine(p, pNext, Color.red, 1.0f);
            p = pNext;
        }
#endif

    }



    private Vector3 FallbackFixedAngle(Transform player)
    {
        Vector3 look = aimAssist
            ? aimAssist.GetAimDirection()
            : (player ? player.forward : Vector3.forward);
        Vector3 horiz = Vector3.ProjectOnPlane(look, Vector3.up).normalized;
        float rad = LaunchAngleDegrees * Mathf.Deg2Rad;
        return horiz * (throwSpeed * Mathf.Cos(rad)) + Vector3.up * (throwSpeed * Mathf.Sin(rad));
    }

    // 固定初速度 s，從 origin 擲到 target 的 v0（可選高/低拋）
    private static bool TrySolveBallisticBest(
        Vector3 origin, Vector3 target, float speed, float maxPitchDeg,
        out Vector3 bestV0, out float bestPitchDeg, out string failReason)
    {
        bestV0 = Vector3.zero;
        bestPitchDeg = 0f;
        failReason = "";

        Vector3 to = target - origin;
        Vector3 toXZ = Vector3.ProjectOnPlane(to, Vector3.up);
        float x = toXZ.magnitude;
        float y = to.y;
        float g = Physics.gravity.y; // negative

        // 極近距：直接直射，避免 0 除
        const float MinHoriz = 0.25f;
        if (x < MinHoriz)
        {
            Vector3 dir = (to.sqrMagnitude > 1e-6f) ? to.normalized : Vector3.forward;
            bestV0 = dir * speed;
            bestPitchDeg = 0f; // 不重要
            return true;
        }

        float s2 = speed * speed;
        float under = s2 * s2 - g * (g * x * x + 2f * y * s2);
        if (under < 0f)
        {
            failReason = "discriminant<0 (speed too low or height gap too large)";
            return false;
        }

        float sqrt = Mathf.Sqrt(under);

        // 兩個解：高拋(+)、低拋(-)
        bool haveAny = false;
        Vector3 candidateV0High = Vector3.zero;
        float pitchHigh = float.MaxValue;
        Vector3 candidateV0Low = Vector3.zero;
        float pitchLow = float.MaxValue;

        // helper：由 tanθ 算向量與 pitch
        bool BuildCandidate(float tan, out Vector3 v0, out float pitchDeg)
        {
            float cos = 1f / Mathf.Sqrt(1f + tan * tan);
            float sin = tan * cos;
            Vector3 dirXZ = toXZ / x;
            v0 = dirXZ * (speed * cos) + Vector3.up * (speed * sin);

            float horizMag = (speed * cos);
            pitchDeg = Mathf.Rad2Deg * Mathf.Atan2(speed * sin, Mathf.Max(1e-5f, horizMag));
            return true;
        }

        // g < 0，所以分母取 (-g * x)
        float tanHigh = (s2 + sqrt) / (-g * x);
        float tanLow = (s2 - sqrt) / (-g * x);

        BuildCandidate(tanHigh, out candidateV0High, out pitchHigh);
        BuildCandidate(tanLow, out candidateV0Low, out pitchLow);

        // 先嘗試選擇「pitch <= 上限」中較小的那個；都超過就失敗（交給外面 fallback）
        bool highOk = pitchHigh <= maxPitchDeg;
        bool lowOk = pitchLow <= maxPitchDeg;

        if (lowOk && (!highOk || pitchLow <= pitchHigh))
        {
            bestV0 = candidateV0Low;
            bestPitchDeg = pitchLow;
            haveAny = true;
        }
        else if (highOk)
        {
            bestV0 = candidateV0High;
            bestPitchDeg = pitchHigh;
            haveAny = true;
        }

        if (!haveAny)
        {
            failReason = $"both pitches too steep (low={pitchLow:F1}°, high={pitchHigh:F1}° > {maxPitchDeg}°)";
            return false;
        }

        return true;
    }
}