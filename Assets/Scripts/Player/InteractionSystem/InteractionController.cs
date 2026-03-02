using System.Collections.Generic;
using UnityEngine;
using Player.InteractionSystem;

public enum InteractState { Idle, ReadyToThrow }

public class InteractionController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HandSlot handSlot;
    [SerializeField] private PlayerCollector collector;
    [SerializeField] private AimAssist aimAssist;
    [SerializeField] private Transform throwOrigin;

    [Header("Throw")]
    [SerializeField] private float throwSpeed = 18f;
    [SerializeField] private ThrowingSystem.ThrowArcMode arcMode = ThrowingSystem.ThrowArcMode.ToTarget;
    [SerializeField] private bool preferHighArc = true;
    [SerializeField, Range(5f, 60f)] private float fixedAngle = 35f;

    [Header("Spell (Empty-hand Throw)")]
    [SerializeField] private Rigidbody spellPrefab;
    [SerializeField] private bool allowSpellWhenEmpty = true;
    [SerializeField] private float spellCooldown = 0.2f;
    [SerializeField, Tooltip("空手也開啟 AimAssist 掃描以利瞄準 spell")]
    private bool scanWhenEmptyForSpell = true;

    // 紀錄當前的法術種類
    private SpellType _currentSpellType; 
    private float _lastSpellTime = -999f;

    [Header("Absorb (Hold)")]
    [SerializeField, Tooltip("長按吸收時的掃描間隔秒數")]
    private float absorbTickInterval = 0.12f;

    [SerializeField] private List<ParticleSystem> particleVFX;
    
    public InteractState State { get; private set; } = InteractState.Idle;

    private ThrowingSystem _thrower;
    private Coroutine _absorbRoutine;
    private bool _isAbsorbHeld;
    private bool _prevScanning;

    void Awake()
    {
        if (!handSlot) handSlot = GetComponentInChildren<HandSlot>();

        _thrower = new ThrowingSystem(throwOrigin, throwSpeed, aimAssist)
        {
            ArcMode = arcMode,
            PreferHighArc = preferHighArc,
            LaunchAngleDegrees = fixedAngle,
            OrientToVelocity = true,
            UseGravity = true
        };

        if (collector)
        {
            collector.SetBusyChecker(() => handSlot && handSlot.HasItem);
            collector.SetOnPulledResult(OnAbsorbResult);
        }

        _prevScanning = false;
        SetAimScanningAccordingToState();
    }

    void Update()
    {
        SetAimScanningAccordingToState();
    }

    private void SetAimScanningAccordingToState()
    {
        if (!aimAssist) return;
        bool has = handSlot && handSlot.HasItem;
        bool shouldScan = has || (scanWhenEmptyForSpell && allowSpellWhenEmpty);
        if (shouldScan != _prevScanning)
        {
            aimAssist.SetScanning(shouldScan);
            _prevScanning = shouldScan;
        }
    }

    /// <summary>
    /// 供外部呼叫：切換當前法術種類
    /// </summary>
    public void SetSpellType(SpellType newType)
    {
        _currentSpellType = newType;
    }

    // ====== 長按吸收：開始/結束 ======
    public void Input_StartAbsorbHold()
    {
        particleVFX?.ForEach(p => p.Play());
        if (_isAbsorbHeld) return;

        _isAbsorbHeld = true;
        if (_absorbRoutine == null)
            _absorbRoutine = StartCoroutine(AbsorbHoldLoop());
    }

    public void Input_Drop()
    {
        particleVFX?.ForEach(p => p.Stop());
        _isAbsorbHeld = false;
        State = InteractState.Idle;

        if (collector)
            collector.CancelAllPulls();

        if (_absorbRoutine != null)
        {
            StopCoroutine(_absorbRoutine);
            _absorbRoutine = null;
        }

        if (handSlot && handSlot.HasItem)
            handSlot.Detach();

        SetAimScanningAccordingToState();
    }

    private System.Collections.IEnumerator AbsorbHoldLoop()
    {
        var wait = new WaitForSeconds(absorbTickInterval);
        while (_isAbsorbHeld)
        {
            if (!handSlot.HasItem && collector)
                collector.TryAbsorbOnce();
            yield return wait;
        }
        _absorbRoutine = null;
    }

    // ====== 投擲 / 丟棄 ======
    public void Input_Throw()
    {
        particleVFX?.ForEach(p => p.Stop());

        if (handSlot && handSlot.HasItem)
        {
            _isAbsorbHeld = false;

            var rb = handSlot.Take();
            if (rb)
            {
                rb.GetComponentInParent<IThrowable>()?.OnBeforeThrow();
                _thrower.ThrowExisting(rb, transform);
            }

            State = InteractState.Idle;
            SetAimScanningAccordingToState();
            return;
        }

        if (allowSpellWhenEmpty && spellPrefab)
        {
            TrySpawnAndThrowSpell();
            State = InteractState.Idle;
            SetAimScanningAccordingToState();
        }
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

    private void TrySpawnAndThrowSpell()
    {
        if (spellPrefab == null) return;
        if (Time.time - _lastSpellTime < spellCooldown) return;

        var pos = throwOrigin ? throwOrigin.position : transform.position + transform.forward * 0.5f + Vector3.up;
        var rot = throwOrigin ? throwOrigin.rotation : transform.rotation;

        Rigidbody rb = Instantiate(spellPrefab, pos, rot);

        rb.isKinematic = false;
        rb.useGravity = _thrower.UseGravity;
        rb.detectCollisions = true;

        Spell spellCmp = rb.GetComponent<Spell>();
        // if (spellCmp != null)
        // {
        //     // 將生成的 Spell 屬性覆寫為當前選擇的種類
        //     spellCmp.spellType = _currentSpellType;
        //
        //     // 整合 AimAssist 導引目標
        //     if (aimAssist != null)
        //     {
        //         Transform currentTarget = aimAssist.GetTarget(); 
        //         if (currentTarget != null)
        //         {
        //             spellCmp.SetTarget(currentTarget);
        //         }
        //     }
        // }

        ResetTrails(rb.transform, emittingAfter: true);
        rb.GetComponentInParent<IThrowable>()?.OnBeforeThrow();
    
        _thrower.ThrowExisting(rb, transform);

        _lastSpellTime = Time.time;
    }
    
    static void ResetTrails(Transform root, bool emittingAfter = true)
    {
        var trails = root.GetComponentsInChildren<TrailRenderer>(true);
        foreach (var tr in trails)
        {
            tr.emitting = false;
            tr.Clear();
        }

        if (emittingAfter)
            root.GetComponent<MonoBehaviour>()?.StartCoroutine(EnableTrailsNextFixed(root));
    }

    static System.Collections.IEnumerator EnableTrailsNextFixed(Transform root)
    {
        yield return new WaitForFixedUpdate();
        var trails = root.GetComponentsInChildren<TrailRenderer>(true);
        foreach (var tr in trails) tr.emitting = true;
    }
}