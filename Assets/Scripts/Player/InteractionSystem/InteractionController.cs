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
    [SerializeField, Range(5f,60f)] private float fixedAngle = 35f;

    [Header("Absorb (Hold)")]
    [SerializeField, Tooltip("長按吸收時的掃描間隔秒數")]
    private float absorbTickInterval = 0.12f;

    public InteractState State { get; private set; } = InteractState.Idle;

    private ThrowingSystem _thrower;
    private Coroutine _absorbRoutine;
    private bool _isAbsorbHeld;

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

        // 注入溝通：手上有東西視為忙碌，Collector 就不會再吸
        if (collector)
        {
            collector.SetBusyChecker(() => handSlot.HasItem);
            collector.SetOnPulledResult(OnAbsorbResult);
        }
    }

    // ====== 長按吸收：開始/結束 ======
    public void Input_StartAbsorbHold()
    {
        if (_isAbsorbHeld) return;
        _isAbsorbHeld = true;

        // 若手上已有物，規則是不可再吸，直接忽略（busyChecker 也會擋）
        if (_absorbRoutine == null)
            _absorbRoutine = StartCoroutine(AbsorbHoldLoop());
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
        // 規則：只有「手上有物」時才能投擲
        if (!handSlot.HasItem) return;

        var rb = handSlot.Take();
        if (!rb) { State = InteractState.Idle; return; }

        rb.GetComponentInParent<IThrowable>()?.OnBeforeThrow();
        _thrower.ThrowExisting(rb, transform); // 會優先對目標解彈道；沒有就按當前前方固定角丟
        State = InteractState.Idle;
    }

    public void Input_Drop()
    {
        if (!handSlot.HasItem) return;
        handSlot.Detach(); // 不加速度，直接放地上
        State = InteractState.Idle;
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
}
