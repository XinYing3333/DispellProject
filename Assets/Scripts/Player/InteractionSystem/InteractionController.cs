using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Player.InteractionSystem;
using Cinemachine;
using Player;

public enum InteractState
{
    Idle,
    ReadyToThrow
}

public class InteractionController : MonoBehaviour
{
    [Header("Refs")] [SerializeField] private HandSlot handSlot;
    [SerializeField] private PlayerCollector collector;
    [SerializeField] private AimAssist aimAssist;
    [SerializeField] private Transform throwOrigin;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Ratchet Style Settings")] [SerializeField]
    private LayerMask aimRaycastMask;

    [SerializeField] private float maxAimDistance = 100f;
    [SerializeField] private GameObject crosshairUI;

    [Header("Throw/Spell Stats")] [SerializeField]
    private float throwSpeed = 22f;

    [SerializeField] private ThrowingSystem.ThrowArcMode arcMode = ThrowingSystem.ThrowArcMode.ToTarget;
    [SerializeField] private Rigidbody spellPrefab;
    [SerializeField] private float spellCooldown = 0.15f;

    [Header("Absorb Settings")] [SerializeField]
    private float absorbTickInterval = 0.12f;

    [SerializeField] private List<ParticleSystem> particleVFX;
    [SerializeField] private ParticleSystem throwVFX;


    private ThrowingSystem _thrower;
    private SpellType _currentSpellType;
    private float _lastSpellTime = -999f;
    private bool _isDetectAiming;
    private bool _isAiming;

    private Coroutine _absorbRoutine;
    private bool _isAbsorbHeld;

    private float _weaponHoldTimer = 0f;
    private const float WeaponHoldDuration = 3f;

    public InteractState State { get; private set; } = InteractState.Idle;

    private void Awake()
    {
        _thrower = new ThrowingSystem(throwOrigin, throwSpeed, aimAssist)
        {
            ArcMode = arcMode,
            OrientToVelocity = true,
            UseGravity = true
        };

        if (collector)
        {
            collector.SetBusyChecker(() => handSlot && handSlot.HasItem);
            collector.SetOnPulledResult(OnAbsorbResult);
        }
    }

    private void Update()
    {
        UpdateAimScanning();
    }

    private void UpdateAimScanning()
    {
        if (!aimAssist) return;
        // 瞄準時關閉 AimAssist 掃描，改由準星決定；非瞄準且手中有物時開啟
        bool shouldScan = !_isAiming && (handSlot && (handSlot.HasItem || true));
        aimAssist.SetScanning(shouldScan);
    }

    // ====== 投擲 / 射擊 (Input_Throw) ======
    public void Input_Throw()
    {
        if (Time.time - _lastSpellTime < spellCooldown) return;

        // 停止吸收 VFX
        particleVFX?.ForEach(p => p.Stop());

        Vector3 targetPoint = GetCurrentTargetPoint();

        if (handSlot && handSlot.HasItem)
        {
            _isAbsorbHeld = false;
            ExecuteThrow(handSlot.Take(), targetPoint);
        }
        else if (spellPrefab)
        {
            ExecuteSpell(targetPoint);
        }

        _lastSpellTime = Time.time;
        State = InteractState.Idle;
    }

    private Vector3 GetCurrentTargetPoint()
    {
        // 1. 瞄準模式：從螢幕中心 Raycast
        if (_isAiming)
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimRaycastMask))
            {
                return hit.point;
            }

            return ray.GetPoint(maxAimDistance);
        }

        // 2. 非瞄準模式：使用 AimAssist 鎖定的目標
        if (aimAssist && aimAssist.CurrentTarget)
        {
            return aimAssist.CurrentTarget.GetAimPoint();
        }

        // 3. 預設前方點
        return throwOrigin
            ? (throwOrigin.position + throwOrigin.forward * 20f)
            : (transform.position + transform.forward * 20f);
    }

    private void ExecuteThrow(Rigidbody rb, Vector3 targetPoint)
    {
        if (!rb) return;
        rb.transform.position = throwOrigin.position;
        rb.isKinematic = false;
        rb.GetComponentInParent<IThrowable>()?.OnBeforeThrow();

        _thrower.ThrowToPoint(rb, targetPoint);
        if (throwVFX) throwVFX.Play();
    }

    private void ExecuteSpell(Vector3 targetPoint)
    {
        Rigidbody rb = Instantiate(spellPrefab, throwOrigin.position, throwOrigin.rotation);

        Spell spellCmp = rb.GetComponent<Spell>();
        if (spellCmp != null)
        {
            spellCmp.spellType = _currentSpellType;
            if (aimAssist && aimAssist.CurrentTarget)
                spellCmp.SetTarget(aimAssist.CurrentTarget.transform);
        }

        _thrower.ThrowToPoint(rb, targetPoint);
        if (throwVFX) throwVFX.Play();
    }

    // ====== 吸收邏輯 (Absorb) ======
    public void Input_StartAbsorbHold()
    {
        particleVFX?.ForEach(p => p.Play());
        if (_isAbsorbHeld) return;

        _isAbsorbHeld = true;
        if (_absorbRoutine == null)
            _absorbRoutine = StartCoroutine(AbsorbHoldLoop());
    }

    private IEnumerator AbsorbHoldLoop()
    {
        var wait = new WaitForSeconds(absorbTickInterval);
        while (_isAbsorbHeld)
        {
            if (handSlot && !handSlot.HasItem && collector)
                collector.TryAbsorbOnce();
            yield return wait;
        }

        _absorbRoutine = null;
    }

    // ====== 丟棄與重置 (Input_Drop) ======
    public void Input_Drop()
    {
        particleVFX?.ForEach(p => p.Stop());
        _isAbsorbHeld = false;

        if (_absorbRoutine != null)
        {
            StopCoroutine(_absorbRoutine);
            _absorbRoutine = null;
        }

        if (collector)
            collector.CancelAllPulls();

        if (handSlot && handSlot.HasItem)
            handSlot.Detach();

        State = InteractState.Idle;
    }

    private void OnAbsorbResult(Rigidbody rb, bool wasCollected)
    {
        if (wasCollected)
        {
            State = InteractState.Idle;
            return;
        }

        if (rb && handSlot && handSlot.TryAttach(rb))
        {
            State = InteractState.ReadyToThrow;
            particleVFX?.ForEach(p => p.Stop());
        }
        else
        {
            State = InteractState.Idle;
        }
    }

    public void SetSpellType(SpellType type) => _currentSpellType = type;
}