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

    // FixedAngle fallback
    public float LaunchAngleDegrees { get; set; } = 35f;

    // Ballistic solve options
    public bool PreferHighArc { get; set; } = true;          // ★選高拋或低拋
    public float MinPitchDeg { get; set; } = 12f;            // ★最低仰角，避免貼地
    public float MaxPitchDeg { get; set; } = 70f;            // ★最高仰角上限（避免太陡）
    public bool ClampPitchIfTooLow { get; set; } = true;     // ★若解出來太低，硬抬到 MinPitchDeg

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

    // 1. 物理狀態初始化
    rb.isKinematic = false;
    rb.useGravity = UseGravity;
    rb.detectCollisions = true;

    // 2. 設置出手位置與旋轉
    Vector3 origin = throwOrigin ? throwOrigin.position : rb.position;
    if (throwOrigin)
    {
        rb.position = throwOrigin.position;
        rb.rotation = throwOrigin.rotation;
    }

    Vector3 v0;

    // 3. 判斷是否滿足輔助瞄準條件
    // 條件：模式為 ToTarget + AimAssist 正在掃描 + 輔助模式為 ThrowableReady + 確有目標
    bool canUseAimAssist = ArcMode == ThrowArcMode.ToTarget && 
                           aimAssist != null && 
                           aimAssist.Scanning && 
                           aimAssist.assistMode == TargetState.ThrowableReady &&
                           aimAssist.CurrentTarget != null;

    if (canUseAimAssist)
    {
        // 取得 Targetable 腳本定義的精確瞄準點 (考慮了 Renderer Bounds 或 Anchor)
        Vector3 targetPoint = aimAssist.CurrentTarget.GetAimPoint();

        bool ok = TrySolveBallisticWithPitchLimits(
            origin, targetPoint, throwSpeed,
            PreferHighArc, MinPitchDeg, MaxPitchDeg, ClampPitchIfTooLow,
            out v0, out float usedPitchDeg, out string reason);

#if UNITY_EDITOR
        if (ok) Debug.Log($"[Throw] Ballistic Success: Pitch={usedPitchDeg:F1}°, Target={aimAssist.CurrentTarget.name}");
        else    Debug.Log($"[Throw] Ballistic Fallback: {reason}");
#endif
        // 若彈道解算失敗（如太遠），則退回到固定角度投擲
        if (!ok) v0 = FallbackFixedAngle(player);
    }
    else
    {
        // 無目標或非投擲模式，直接使用固定角度前向投擲
        v0 = FallbackFixedAngle(player);
    }

    // 4. 應用速度 (兼容 Unity 6)
#if UNITY_6000_0_OR_NEWER
    rb.linearVelocity = v0;
#else
    rb.velocity = v0;
#endif

    // 5. 視覺修正：讓物件朝向飛行方向
    if (OrientToVelocity && v0.sqrMagnitude > 1e-4f)
        rb.transform.rotation = Quaternion.LookRotation(v0.normalized, Vector3.up);

#if UNITY_EDITOR
    DrawDebugTrajectory(origin, v0);
#endif
}

// 抽離 Debug 繪製邏輯
private void DrawDebugTrajectory(Vector3 origin, Vector3 velocity)
{
    Vector3 p = origin;
    Vector3 gAcc = Physics.gravity;
    float step = 0.02f;
    for (float t = 0; t < 1.0f; t += step)
    {
        Vector3 pNext = origin + velocity * t + 0.5f * gAcc * (t * t);
        Debug.DrawLine(p, pNext, Color.red, 1.0f);
        p = pNext;
    }
}

    private Vector3 FallbackFixedAngle(Transform player)
    {
        Vector3 look = aimAssist
            ? aimAssist.GetAimDirection()
            : (player ? player.forward : Vector3.forward);

        Vector3 horiz = Vector3.ProjectOnPlane(look, Vector3.up);
        if (horiz.sqrMagnitude < 1e-6f) horiz = Vector3.forward;
        horiz.Normalize();

        float angle = Mathf.Max(LaunchAngleDegrees, MinPitchDeg);
        float rad = angle * Mathf.Deg2Rad;

        return horiz * (throwSpeed * Mathf.Cos(rad)) + Vector3.up * (throwSpeed * Mathf.Sin(rad));
    }

    private static bool TrySolveBallisticWithPitchLimits(
        Vector3 origin, Vector3 target, float speed,
        bool preferHighArc,
        float minPitchDeg, float maxPitchDeg,
        bool clampIfTooLow,
        out Vector3 v0, out float usedPitchDeg, out string failReason)
    {
        v0 = Vector3.zero;
        usedPitchDeg = 0f;
        failReason = "";

        Vector3 to = target - origin;
        Vector3 toXZ = Vector3.ProjectOnPlane(to, Vector3.up);
        float x = toXZ.magnitude;
        float y = to.y;

        // 極近距：直接朝向（再做最小仰角）
        const float MinHoriz = 0.25f;
        if (x < MinHoriz)
        {
            Vector3 dir = (to.sqrMagnitude > 1e-6f) ? to.normalized : Vector3.forward;
            v0 = dir * speed;
            v0 = EnforceMinPitch(v0, minPitchDeg);
            usedPitchDeg = PitchDeg(v0);
            return true;
        }

        float gMag = -Physics.gravity.y; // 正值
        if (gMag <= 0.0001f)
        {
            failReason = "gravity invalid";
            return false;
        }

        float v2 = speed * speed;
        float v4 = v2 * v2;

        // disc = v^4 - g (g x^2 + 2 y v^2)
        float disc = v4 - gMag * (gMag * x * x + 2f * y * v2);
        if (disc < 0f)
        {
            failReason = "discriminant<0 (speed too low or height gap too large)";
            return false;
        }

        float sqrt = Mathf.Sqrt(disc);

        // tanθ 兩解（g 正值）
        float tanLow  = (v2 - sqrt) / (gMag * x);
        float tanHigh = (v2 + sqrt) / (gMag * x);

        BuildCandidate(origin, toXZ, x, speed, tanLow,  out Vector3 vLow,  out float pitchLow);
        BuildCandidate(origin, toXZ, x, speed, tanHigh, out Vector3 vHigh, out float pitchHigh);

        bool lowOk  = pitchLow  <= maxPitchDeg;
        bool highOk = pitchHigh <= maxPitchDeg;

        // 依偏好選解：先選 Prefer 的；不行再退另一個
        bool picked = false;
        if (preferHighArc && highOk)
        {
            v0 = vHigh; usedPitchDeg = pitchHigh; picked = true;
        }
        else if (!preferHighArc && lowOk)
        {
            v0 = vLow; usedPitchDeg = pitchLow; picked = true;
        }
        else if (highOk)
        {
            v0 = vHigh; usedPitchDeg = pitchHigh; picked = true;
        }
        else if (lowOk)
        {
            v0 = vLow; usedPitchDeg = pitchLow; picked = true;
        }

        if (!picked)
        {
            failReason = $"both pitches too steep (low={pitchLow:F1}°, high={pitchHigh:F1}° > {maxPitchDeg:F1}°)";
            return false;
        }

        // ★最小仰角：解出來太低就抬高（維持速度大小）
        if (clampIfTooLow)
        {
            v0 = EnforceMinPitch(v0, minPitchDeg);
            usedPitchDeg = PitchDeg(v0);
        }
        else
        {
            // 不硬抬時：若 pitch < minPitch 直接判定失敗（交給 fallback）
            if (usedPitchDeg < minPitchDeg)
            {
                failReason = $"pitch too low ({usedPitchDeg:F1}° < {minPitchDeg:F1}°)";
                return false;
            }
        }

        return true;
    }

    private static void BuildCandidate(Vector3 origin, Vector3 toXZ, float x, float speed, float tan,
        out Vector3 v0, out float pitchDeg)
    {
        float cos = 1f / Mathf.Sqrt(1f + tan * tan);
        float sin = tan * cos;

        Vector3 dirXZ = toXZ / x;

        float vHoriz = speed * cos;
        float vY = speed * sin;

        v0 = dirXZ * vHoriz + Vector3.up * vY;
        pitchDeg = Mathf.Rad2Deg * Mathf.Atan2(vY, Mathf.Max(1e-5f, vHoriz));
    }

    private static float PitchDeg(Vector3 v)
    {
        float flat = new Vector2(v.x, v.z).magnitude;
        return Mathf.Atan2(v.y, Mathf.Max(1e-5f, flat)) * Mathf.Rad2Deg;
    }

    private static Vector3 EnforceMinPitch(Vector3 v, float minPitchDeg)
    {
        float speed = v.magnitude;
        if (speed < 1e-5f) return v;

        float flat = new Vector2(v.x, v.z).magnitude;
        if (flat < 1e-6f) return v; // 幾乎垂直，不調

        float pitch = Mathf.Atan2(v.y, flat) * Mathf.Rad2Deg;
        if (pitch >= minPitchDeg) return v;

        float pitchRad = minPitchDeg * Mathf.Deg2Rad;

        float newY = speed * Mathf.Sin(pitchRad);
        float newFlat = speed * Mathf.Cos(pitchRad);

        Vector2 xz = new Vector2(v.x, v.z).normalized * newFlat;
        return new Vector3(xz.x, newY, xz.y);
    }
    
    public void ThrowToPoint(Rigidbody rb, Vector3 targetPoint)
    {
        if (!rb) return;
        rb.isKinematic = false;
    
        Vector3 origin = throwOrigin ? throwOrigin.position : rb.position;
    
        // 使用你原本寫好的彈道解算器，直接餵入 targetPoint
        bool ok = TrySolveBallisticWithPitchLimits(
            origin, targetPoint, throwSpeed, 
            PreferHighArc, MinPitchDeg, MaxPitchDeg, true, 
            out Vector3 v0, out _, out _);

        if (!ok) v0 = (targetPoint - origin).normalized * throwSpeed; // 解不出來就直線射擊

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v0;
#else
    rb.velocity = v0;
#endif
    }
}
