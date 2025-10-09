using System.Collections.Generic;
using NUnit.Framework;
using Player.InteractionSystem;
using UnityEngine;

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
    [SerializeField, UnityEngine.Range(5f,60f)] private float fixedAngle = 35f;
    
    [Header("Spell (Empty-hand Throw)")]
    [SerializeField] private Rigidbody spellPrefab;     // ★ 你要丟的法術 Prefab（上面要有 Rigidbody）
    [SerializeField] private bool allowSpellWhenEmpty = true;
    [SerializeField] private float spellCooldown = 0.2f;
    [SerializeField, Tooltip("空手也開啟 AimAssist 掃描以利瞄準 spell")]
    private bool scanWhenEmptyForSpell = true;

    private float _lastSpellTime = -999f;

    [Header("Absorb (Hold)")]
    [SerializeField, Tooltip("長按吸收時的掃描間隔秒數")]
    private float absorbTickInterval = 0.12f;

    [SerializeField]private List<ParticleSystem> particleVFX;
    public InteractState State { get; private set; } = InteractState.Idle;

    private ThrowingSystem _thrower;
    private Coroutine _absorbRoutine;
    private bool _isAbsorbHeld;
    private bool _prevHasItem;

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

        if (!collector) return;
        collector.SetBusyChecker(() => handSlot.HasItem);
        collector.SetOnPulledResult(OnAbsorbResult);

        _prevHasItem = handSlot && handSlot.HasItem;
        if (aimAssist) aimAssist.SetScanning(_prevHasItem || scanWhenEmptyForSpell); // ★ 空手也可掃描（可關）
    }
    
    void Update()
    {
        if (handSlot && aimAssist)
        {
            bool has = handSlot.HasItem;
            if(has)particleVFX.ForEach(particle => particle.Stop());
            // ★ 若允許空手施放 spell，則空手時也維持掃描；否則維持原本「手上有東西才掃描」
            bool shouldScan = has || (scanWhenEmptyForSpell && allowSpellWhenEmpty);
            if (shouldScan != _prevHasItem) // 用 _prevHasItem 當前狀態對比旗標
            {
                aimAssist.SetScanning(shouldScan);
                _prevHasItem = shouldScan;
            }
        }
        
    }

    // ====== 長按吸收：開始/結束 ======
    public void Input_StartAbsorbHold()
    {
        particleVFX.ForEach(particle => particle.Play());

        if (_isAbsorbHeld) return;
        _isAbsorbHeld = true;

        // 若手上已有物，規則是不可再吸，直接忽略（busyChecker 也會擋）
        if (_absorbRoutine == null)
            _absorbRoutine = StartCoroutine(AbsorbHoldLoop());
    }

    public void Input_Drop()
    {
        particleVFX.ForEach(particle => particle.Stop());

        _isAbsorbHeld = false;
        State = InteractState.Idle;
        
        if (!handSlot.HasItem) return;
        handSlot.Detach(); // 不加速度，直接放地上
    }
    
    public void Input_StopAbsorbHold()
    {
        _isAbsorbHeld = false;
        if (_absorbRoutine != null)
        {
            StopCoroutine(_absorbRoutine);
            _absorbRoutine = null;
        }
        // 放開後狀態：有持有物 → ReadyToThrow，否則 Idle
        State = handSlot.HasItem ? InteractState.ReadyToThrow : InteractState.Idle;
    }

    private System.Collections.IEnumerator AbsorbHoldLoop()
    {
        var wait = new WaitForSeconds(absorbTickInterval);
        while (_isAbsorbHeld)
        {
            // 僅在未持有物時嘗試吸收一次
            if (!handSlot.HasItem && collector)
                collector.TryAbsorbOnce();   // 一次決策：收進背包或交給手上

            // 若此 tick 吸到手上物，下一輪會因 handSlot.HasItem=true 而不再嘗試
            yield return wait;
        }
        _absorbRoutine = null;
    }

    // ====== 投擲 / 丟棄 ======
    public void Input_Throw()
    {
        particleVFX.ForEach(particle => particle.Stop());

        // 先處理「手上有物」的既有流程
        if (handSlot.HasItem)
        {
            _isAbsorbHeld = false;

            var rb = handSlot.Take();
            if (!rb) { State = InteractState.Idle; return; }

            rb.GetComponentInParent<IThrowable>()?.OnBeforeThrow();
            _thrower.ThrowExisting(rb, transform);
            State = InteractState.Idle;
            return;
        }

        // ★ 新增：手上「沒有物件」→ 丟 spell
        if (allowSpellWhenEmpty && spellPrefab)
        {
            TrySpawnAndThrowSpell();
            State = InteractState.Idle;
        }
        // else: 沒有 spellPrefab 或未允許，就什麼都不做
    }

    // ====== 由 Collector 回報的吸收結果 ======
    private void OnAbsorbResult(Rigidbody rb, bool wasCollected)
    {
        if (wasCollected)
        {
            // 直接收進背包 → 如果仍在按住，保持吸收狀態；否則 Idle
            State = _isAbsorbHeld ? InteractState.Idle : InteractState.Idle;
            return;
        }

        // 吸到不可收集的剛體 → 放到手上，進入可投擲狀態
        if (rb && handSlot.TryAttach(rb))
            State = InteractState.ReadyToThrow;
        else
            State = InteractState.Idle;
    }
    
    private void TrySpawnAndThrowSpell()
    {
        if (Time.time - _lastSpellTime < spellCooldown) return;

        // 生成實例
        Rigidbody rb = Instantiate(spellPrefab);

        // 設定初始位置/朝向
        if (throwOrigin)
        {
            rb.position = throwOrigin.position;
            rb.rotation = throwOrigin.rotation;
        }
        else
        {
            // 沒指定 throwOrigin 就用玩家前方一小段
            rb.position = transform.position + transform.forward * 0.5f + Vector3.up * 1f;
            rb.rotation = transform.rotation;
        }

        // 物理開啟（確保可以飛）
        rb.isKinematic = false;
        rb.useGravity  = _thrower.UseGravity;
        rb.detectCollisions = true;

        // 可選：若 spell 也實作 IThrowable，仍可觸發統一鉤子
        rb.GetComponentInParent<IThrowable>()?.OnBeforeThrow();

        // 直接沿用你的彈道系統（含 AimAssist → ToTarget、或 fallback 固定角）
        _thrower.ThrowExisting(rb, transform);

        _lastSpellTime = Time.time;
    }

}
