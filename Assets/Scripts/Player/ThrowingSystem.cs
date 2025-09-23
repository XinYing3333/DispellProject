// ThrowingSystem.cs
using UnityEngine;

public class ThrowingSystem
{
    public enum ThrowArcMode { FixedAngle, ToTarget }

    private GameObject throwablePrefab;
    private GameObject spellPrefab;
    private Transform throwOrigin;
    private float throwSpeed;                 // = 初速度(m/s)，以速度發射
    private AimAssist aimAssist;

    // ===== 可調參數（也可改成建構子參數） =====
    public ThrowArcMode ArcMode { get; set; } = ThrowArcMode.FixedAngle;
    public float LaunchAngleDegrees { get; set; } = 35f;   // FixedAngle 模式用
    public bool PreferHighArc { get; set; } = true;        // ToTarget 模式：高拋/低拋
    public bool OrientToVelocity { get; set; } = true;     // 讓模型朝向速度
    public bool UseGravity { get; set; } = true;           // 開啟重力（拋物線關鍵）

    // 舊的 throwForce 其實是在當速度用；為避免混淆，統一叫 throwSpeed
    public ThrowingSystem(GameObject throwablePrefab, GameObject spellPrefab, Transform throwOrigin, float throwForce, AimAssist aimAssist = null)
    {
        this.throwablePrefab = throwablePrefab;
        this.spellPrefab = spellPrefab;
        this.throwOrigin = throwOrigin;
        this.throwSpeed = throwForce; // 相容你現有代碼；可改名為 throwSpeed
        this.aimAssist = aimAssist;
    }

    public void SetThrowSpeed(float speed) => throwSpeed = Mathf.Max(0f, speed);

    public void ThrowObject(Transform player)
    {
        GameObject selectedPrefab = spellPrefab != null ? spellPrefab : throwablePrefab;
        if (!selectedPrefab)
        {
            Debug.LogError("[ThrowingSystem] 沒有可用的投擲 Prefab。");
            return;
        }

        GameObject go = GameObject.Instantiate(selectedPrefab, throwOrigin.position, throwOrigin.rotation);
        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (!rb)
        {
            Debug.LogError("[ThrowingSystem] Prefab 沒有 Rigidbody，無法投擲。");
            return;
        }

        rb.useGravity = UseGravity; // 讓重力參與 => 才會有拋物線

        // 視線方向（備援）
        Vector3 lookDir = (aimAssist != null) ? aimAssist.GetAimDirection()
                         : (player ? player.forward : Vector3.forward);
        if (lookDir.sqrMagnitude < 1e-6f) lookDir = (player ? player.forward : Vector3.forward);
        lookDir.Normalize();

        // 計算初速度 v0
        Vector3 v0;
        bool solved = false;

        if (ArcMode == ThrowArcMode.ToTarget && aimAssist != null && aimAssist.CurrentTarget != null)
        {
            Vector3 targetPos = aimAssist.CurrentTarget.GetAimPoint(); // 你 Targetable 已經有 GetAimPoint()
            solved = TrySolveBallisticVelocity(throwOrigin.position, targetPos, throwSpeed, out v0, PreferHighArc);
        }
        else
        {
            solved = false;
            v0 = Vector3.zero; // 先給值，下面會用 FixedAngle 回退
        }

        if (!solved)
        {
            // 回退：固定拋角
            Vector3 horizDir = Vector3.ProjectOnPlane(lookDir, Vector3.up).normalized;
            if (horizDir.sqrMagnitude < 1e-6f) horizDir = (player ? player.forward : Vector3.forward);

            float rad = LaunchAngleDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            v0 = horizDir * (throwSpeed * cos) + Vector3.up * (throwSpeed * sin);
        }

        // 設定剛體速度（Unity 6 可用 linearVelocity；若報 API 錯誤改用 rb.velocity）
        rb.linearVelocity = v0;

        if (OrientToVelocity && v0.sqrMagnitude > 1e-4f)
            go.transform.rotation = Quaternion.LookRotation(v0.normalized, Vector3.up);

        // （可選）忽略與玩家的一次碰撞，避免出手就撞到自己
        // var playerCol = player ? player.GetComponentInChildren<Collider>() : null;
        // var bulletCol = go.GetComponentInChildren<Collider>();
        // if (playerCol && bulletCol) Physics.IgnoreCollision(playerCol, bulletCol, true);

#if UNITY_EDITOR
        if (ArcMode == ThrowArcMode.ToTarget && aimAssist && aimAssist.CurrentTarget)
            Debug.Log($"[ThrowingSystem] ToTarget 發射，目標：{aimAssist.CurrentTarget.name}，" +
                      $"{(PreferHighArc ? "高拋" : "低拋")}，速度={throwSpeed:F1} m/s");
        else
            Debug.Log($"[ThrowingSystem] FixedAngle 發射，角度={LaunchAngleDegrees:F1}°，速度={throwSpeed:F1} m/s");
#endif
    }

    /// <summary>
    /// 固定初速度 speed，解出從 origin 擲到 target 的初速度向量 v0（可選高拋/低拋）。
    /// 返回 false 表示 speed 太低或幾何條件無解（例如距離過遠、落差過大）。
    /// </summary>
    private static bool TrySolveBallisticVelocity(Vector3 origin, Vector3 target, float speed, out Vector3 v0, bool preferHighArc)
    {
        v0 = Vector3.zero;

        Vector3 toTarget = target - origin;
        Vector3 toTargetXZ = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        float x = toTargetXZ.magnitude;          // 地面水平距離
        float y = toTarget.y;                    // 垂直高度差（目標在上為正）
        float g = Physics.gravity.y;             // Unity 中重力 y 通常為 -9.81
        float s2 = speed * speed;

        // 根號判斷：<0 無解
        float underSqrt = s2 * s2 - g * (g * x * x + 2f * y * s2);
        if (underSqrt < 0f) return false;

        float sqrt = Mathf.Sqrt(underSqrt);

        // tanθ = (s^2 ± sqrt) / (g * x)
        // 注意：g 為負值
        float tanTheta = (preferHighArc ? (s2 + sqrt) : (s2 - sqrt)) / (-g * x);

        float cosTheta = 1f / Mathf.Sqrt(1f + tanTheta * tanTheta);
        float sinTheta = tanTheta * cosTheta;

        Vector3 dirXZ = (x > 1e-4f) ? (toTargetXZ / x) : Vector3.forward; // 地面方向單位向量
        v0 = dirXZ * (speed * cosTheta) + Vector3.up * (speed * sinTheta);
        return true;
    }
}
