// JumpFrictionBypassLite.cs
// 目的：只在「跳躍中」暫時關閉牆面摩擦，其他行為完全不碰。
// 設計：優先讀 PlayerMovement 的現有欄位/方法（反射快取），失敗才用精簡 fallback。

using System;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class JumpFrictionBypassLite : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("起跳後零摩擦持續時間（秒）")]
    [SerializeField] private float noFrictionSeconds = 0.12f;

    [Tooltip("空中若再次獲得向上動量（如雙跳），是否刷新零摩擦時間")]
    [SerializeField] private bool refreshOnUpwardImpulse = true;

    [Tooltip("判定為再次向上動量的最小 ΔVy")]
    [SerializeField] private float impulseThreshold = 1.0f;

    [Header("Manual Override（通常保持空白即可）")]
    [Tooltip("若未能自動讀取，才手動指定；留空則不變更回復材質")]
    [SerializeField] private PhysicsMaterial defaultMaterialOverride;
    [Tooltip("若未能自動讀取，才手動指定；需要 Static/Dynamic=0, Combine=Minimum")]
    [SerializeField] private PhysicsMaterial noFrictionMaterialOverride;
    [Tooltip("若未能讀 IsGrounded() 時，fallback 的地面層")]
    [SerializeField] private LayerMask fallbackGroundMask;
    [Tooltip("fallback 射線：由角色中心往上偏移")]
    [SerializeField] private float fbRayStart = 0.1f;
    [Tooltip("fallback 射線長度")]
    [SerializeField] private float fbRayLen = 0.2f;

    // --- 快取 ---
    private Rigidbody _rb;
    private Collider[] _cols;

    // 從 PlayerMovement 反射快取
    private Component _pm;                               // PlayerMovement 實例
    private Func<bool> _pmIsGrounded;                    // 委派：呼叫 PlayerMovement.IsGrounded()
    private LayerMask? _pmGroundMask;
    private PhysicsMaterial _pmDefaultMat;
    private PhysicsMaterial _pmNoFrictionMat;

    // 狀態
    private bool _wasGrounded;
    private bool _active;                                // 是否正使用零摩擦
    private float _timer;
    private float _lastVelY;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _cols = GetComponentsInChildren<Collider>(includeInactive: false);

        // === 反射讀 PlayerMovement ===
        var pmType = typeof(MonoBehaviour).Assembly.GetType("PlayerMovement"); // 直接找型別名
        _pm = GetComponent(pmType ?? typeof(Component));
        if (_pm == null)
        {
            // 若類名不在預設程式集，改用遍歷找
            foreach (var c in GetComponents<MonoBehaviour>())
            {
                if (c != null && c.GetType().Name == "PlayerMovement") { _pm = c; break; }
            }
        }

        if (_pm != null)
        {
            var t = _pm.GetType();

            // 1) 方法 IsGrounded() → 建委派
            try
            {
                var mi = t.GetMethod("IsGrounded", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (mi != null && mi.GetParameters().Length == 0 && mi.ReturnType == typeof(bool))
                {
                    _pmIsGrounded = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), _pm, mi);
                }
            }
            catch { /* 忽略，走 fallback */ }

            // 2) 欄位 groundLayer（private serialized）
            try
            {
                var fi = t.GetField("groundLayer", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (fi != null && fi.FieldType == typeof(LayerMask))
                    _pmGroundMask = (LayerMask)fi.GetValue(_pm);
            }
            catch { /* 忽略 */ }

            // 3) 欄位 defaultMaterial / noFrictionMaterial（private serialized）
            try
            {
                var fiDefault = t.GetField("defaultMaterial", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (fiDefault != null) _pmDefaultMat = fiDefault.GetValue(_pm) as PhysicsMaterial;

                var fiNoFric = t.GetField("noFrictionMaterial", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (fiNoFric != null) _pmNoFrictionMat = fiNoFric.GetValue(_pm) as PhysicsMaterial;
            }
            catch { /* 忽略 */ }
        }

        // 安全提示
        var useNoFric = _pmNoFrictionMat ?? noFrictionMaterialOverride;
        if (useNoFric != null && (useNoFric.dynamicFriction != 0f || useNoFric.staticFriction != 0f))
            Debug.LogWarning("[JumpFrictionBypassLite] 建議 noFrictionMaterial 的 Static/Dynamic Friction 設為 0，Combine 設為 Minimum。");
    }

    private void OnDisable()
    {
        if (_active) ApplyMaterial(GetDefaultMaterialOrNull());
        _active = false;
        _timer = 0f;
    }

    private void FixedUpdate()
    {
        bool grounded = IsGroundedSmart();

        // 起跳判定：上一幀在地面、這幀離地，且速度朝上
        if (_wasGrounded && !grounded && _rb.linearVelocity.y > 0.01f)
            ActivateNoFriction();

        // 空中再次獲得向上動量（例如雙跳），可刷新
        float vy = _rb.linearVelocity.y;
        if (refreshOnUpwardImpulse && !grounded && (vy - _lastVelY) >= impulseThreshold)
            ActivateNoFriction();

        // 計時與還原
        if (_active)
        {
            _timer -= Time.fixedDeltaTime;
            if (grounded || _timer <= 0f)
                DeactivateNoFriction();
        }

        _wasGrounded = grounded;
        _lastVelY = vy;
    }

    // --- 核心：優先用 PlayerMovement 的 IsGrounded()；失敗才 fallback ---
    private bool IsGroundedSmart()
    {
        if (_pmIsGrounded != null)
        {
            try { return _pmIsGrounded(); }
            catch { /* 若委派失敗則落回 fallback */ }
        }

        // fallback：射線與 PlayerMovement 內建相同數值（可在 Inspector 調整）
        Vector3 origin = transform.position + Vector3.up * fbRayStart;
        LayerMask mask = _pmGroundMask ?? fallbackGroundMask;
        return Physics.Raycast(origin, Vector3.down, fbRayLen, mask, QueryTriggerInteraction.Ignore);
    }

    private void ActivateNoFriction()
    {
        var noFric = _pmNoFrictionMat ?? noFrictionMaterialOverride;
        if (noFric == null) return;

        _timer = Mathf.Max(_timer, noFrictionSeconds);
        if (_active) return;

        ApplyMaterial(noFric);
        _active = true;
    }

    private void DeactivateNoFriction()
    {
        ApplyMaterial(GetDefaultMaterialOrNull());
        _active = false;
        _timer = 0f;
    }

    private PhysicsMaterial GetDefaultMaterialOrNull()
    {
        return _pmDefaultMat ?? defaultMaterialOverride; // 允許為 null → 不強制改回
    }

    private void ApplyMaterial(PhysicsMaterial matOrNull)
    {
        foreach (var c in _cols)
        {
            if (!c || c.isTrigger) continue;
            c.material = matOrNull; // 允許 null：代表不變更/還原到 Collider 原本的設定
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 僅顯示 fallback 的射線（實際用的是 PM 的 IsGrounded 就不會畫）
        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position + Vector3.up * fbRayStart;
        Gizmos.DrawLine(origin, origin + Vector3.down * fbRayLen);
    }
#endif
}
