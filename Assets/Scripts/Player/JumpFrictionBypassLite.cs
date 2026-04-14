using System;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class JumpFrictionBypassLite : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField, Tooltip("起跳後零摩擦持續時間（秒）")]
    private float noFrictionSeconds = 0.12f;

    [SerializeField, Tooltip("空中若再次獲得向上動量（如雙跳），是否刷新零摩擦時間")]
    private bool refreshOnUpwardImpulse = true;

    [SerializeField, Tooltip("判定為再次向上動量的最小 ΔVy")]
    private float impulseThreshold = 1.0f;

    [Header("Slope 判斷")]
    [SerializeField, Tooltip("小於這個坡度就當作正常地面，不要啟動零摩擦 (度)")]
    private float maxGroundSlopeDeg = 44f;   // 你角色能站的坡度，依你角色調

    [Header("Manual Override（通常保持空白即可）")]
    [SerializeField] private PhysicsMaterial defaultMaterialOverride;
    [SerializeField] private PhysicsMaterial noFrictionMaterialOverride;
    [SerializeField] private LayerMask fallbackGroundMask;
    [SerializeField] private float fbRayStart = 0.1f;
    [SerializeField] private float fbRayLen = 0.2f;

    private Rigidbody _rb;
    private Collider[] _cols;

    private Component _pm;
    private Func<bool> _pmIsGrounded;
    private LayerMask? _pmGroundMask;
    private PhysicsMaterial _pmDefaultMat;
    private PhysicsMaterial _pmNoFrictionMat;

    private bool _wasGrounded;
    private bool _active;
    private float _timer;
    private float _lastVelY;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _cols = GetComponentsInChildren<Collider>(false);

        _pm = GetComponent<PlayerMovement>();

        if (_pm != null)
        {
            var t = _pm.GetType();

            // IsGrounded()
            try
            {
                var mi = t.GetMethod("IsGrounded", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (mi != null && mi.GetParameters().Length == 0 && mi.ReturnType == typeof(bool))
                    _pmIsGrounded = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), _pm, mi);
            }
            catch { }

            // groundLayer
            try
            {
                var fi = t.GetField("groundLayer", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (fi != null && fi.FieldType == typeof(LayerMask))
                    _pmGroundMask = (LayerMask)fi.GetValue(_pm);
            }
            catch { }

            // default / no friction mat
            try
            {
                var fiDefault = t.GetField("defaultMaterial", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (fiDefault != null) _pmDefaultMat = fiDefault.GetValue(_pm) as PhysicsMaterial;

                var fiNoFric = t.GetField("noFrictionMaterial", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (fiNoFric != null) _pmNoFrictionMat = fiNoFric.GetValue(_pm) as PhysicsMaterial;
            }
            catch { }
        }

        var useNoFric = _pmNoFrictionMat ?? noFrictionMaterialOverride;
        if (useNoFric != null && (useNoFric.dynamicFriction != 0f || useNoFric.staticFriction != 0f))
        {
            Debug.LogWarning("[JumpFrictionBypassLite] 建議 noFrictionMaterial 的 Static/Dynamic Friction 設為 0，Combine 設為 Minimum。");
        }
    }

    private void OnDisable()
    {
        if (_active) ApplyMaterial(GetDefaultMaterialOrNull());
        _active = false;
        _timer = 0f;
    }

    private void FixedUpdate()
    {
        bool grounded = IsGroundedSmart(out float slopeDeg);
        float vy = _rb.linearVelocity.y;

        // 如果不在地面，且不是站在「可站立的斜坡」上，就啟用零摩擦
        if (!grounded)
        {
            if (!IsOnStandableSlope())
            {
                ActivateNoFriction();
            }
            else
            {
                // 如果雖然判定為空中，但射線打到了可站立的斜坡（可能只是微小浮空），則恢復摩擦力
                DeactivateNoFriction();
            }
        }
        else
        {
            // 在地面上，恢復正常摩擦力
            DeactivateNoFriction();
        }

        _wasGrounded = grounded;
        _lastVelY = vy;
    }

    // 移除原本依賴 Timer 的 Deactivate 邏輯，改為直接切換
    private void ActivateNoFriction()
    {
        var noFric = _pmNoFrictionMat ?? noFrictionMaterialOverride;
        if (noFric == null) return;

        if (_active) return;

        ApplyMaterial(noFric);
        _active = true;
    }

    private void DeactivateNoFriction()
    {
        if (!_active) return;
        
        ApplyMaterial(GetDefaultMaterialOrNull());
        _active = false;
    }
    // -------------------------------------------------
    // 地面偵測：回傳「是否 grounded」+「量到的坡度」
    // -------------------------------------------------
    private bool IsGroundedSmart(out float slopeDeg)
    {
        slopeDeg = 999f;

        // 優先用 PlayerMovement 的
        if (_pmIsGrounded != null)
        {
            try
            {
                bool g = _pmIsGrounded();
                if (g)
                {
                    // 如果 PlayerMovement 自己說有地面，我們再做一次短射線抓法線
                    if (TryRaycastGround(out var hit))
                    {
                        slopeDeg = Vector3.Angle(hit.normal, Vector3.up);
                    }
                }
                return g;
            }
            catch { }
        }

        // fallback
        if (TryRaycastGround(out var fbHit))
        {
            slopeDeg = Vector3.Angle(fbHit.normal, Vector3.up);
            return true;
        }

        return false;
    }

    // 嘗試打一條向下 ray 抓地面 hit
    private bool TryRaycastGround(out RaycastHit hit)
    {
        Vector3 origin = transform.position + Vector3.up * fbRayStart;
        LayerMask mask = _pmGroundMask ?? fallbackGroundMask;
        return Physics.Raycast(origin, Vector3.down, out hit, fbRayLen, mask, QueryTriggerInteraction.Ignore);
    }

    // 判斷「現在」是不是站得住的坡
    private bool IsOnStandableSlope()
    {
        if (TryRaycastGround(out var hit))
        {
            float slopeDeg = Vector3.Angle(hit.normal, Vector3.up);
            return slopeDeg <= maxGroundSlopeDeg;
        }
        return false;
    }
    
    private PhysicsMaterial GetDefaultMaterialOrNull()
    {
        return _pmDefaultMat ?? defaultMaterialOverride;
    }

    private void ApplyMaterial(PhysicsMaterial matOrNull)
    {
        foreach (var c in _cols)
        {
            if (!c || c.isTrigger) continue;
            c.material = matOrNull;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position + Vector3.up * fbRayStart;
        Gizmos.DrawLine(origin, origin + Vector3.down * fbRayLen);
    }
#endif
}
