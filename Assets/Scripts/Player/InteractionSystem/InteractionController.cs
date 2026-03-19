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
     [SerializeField] private RectTransform crosshairRect; // 拖入你的 UI 圖示

    // [SerializeField] private GameObject crosshairUI;

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
    private bool _canCurrentItemBeThrown;

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
    
    private void LateUpdate()
    {
        UpdateAimVisuals();
    }

    private void UpdateAimVisuals()
    {
        bool hasTarget = aimAssist != null && aimAssist.CurrentTarget != null;

        // 1. 準星 UI 邏輯：僅在有目標時顯現
        if (crosshairRect != null)
        {
            if (hasTarget)
            {
                Vector3 targetPoint = aimAssist.CurrentTarget.GetAimPoint();
                Vector3 screenPos = Camera.main.WorldToScreenPoint(targetPoint);

                // 檢查是否在相機前方
                if (screenPos.z > 0)
                {
                    crosshairRect.gameObject.SetActive(true);
                    crosshairRect.position = screenPos;
                
                    float dist = Vector3.Distance(Camera.main.transform.position, targetPoint);
                    float scale = Mathf.Lerp(1.2f, 0.4f, dist / maxAimDistance);
                    crosshairRect.localScale = new Vector3(scale, scale, 1f);
                }
                else
                {
                    crosshairRect.gameObject.SetActive(false);
                }
            }
            else
            {
                crosshairRect.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateAimScanning()
    {
        if (!aimAssist) return;

        // 核心邏輯：
        // 1. 如果手上有東西 -> 切換為投擲模式 (ThrowableReady)，開啟掃描
        // 2. 如果手上沒東西 -> 切換為法術模式 (SpellReady)，開啟掃描
        if (handSlot && handSlot.HasItem)
        {
            aimAssist.SetAssistMode(TargetState.ThrowableReady);
            aimAssist.SetScanning(true);
        }
        else
        {
            aimAssist.SetAssistMode(TargetState.SpellReady);
            aimAssist.SetScanning(true);
        }
    }

    // ====== 投擲 / 射擊 (Input_Throw) ======
    public void Input_Throw()
    {
        if (Time.time - _lastSpellTime < spellCooldown) return;

        if (handSlot && handSlot.HasItem)
        {
            _isAbsorbHeld = false;

            // 如果物件不具備投擲能力（如紅綠燈），點擊發射鍵改為執行 Drop
            if (_canCurrentItemBeThrown)
            {
                if (playerMovement != null) playerMovement.SyncRotationToCameraInstant();
                Vector3 targetPoint = GetCurrentTargetPoint();
                ExecuteThrow(handSlot.Take(), targetPoint);
            }
            else
            {
                Input_Drop(); 
            }
        }
        else if (spellPrefab)
        {
            if (playerMovement != null) playerMovement.SyncRotationToCameraInstant();
            ExecuteSpell(GetCurrentTargetPoint());
        }

        _lastSpellTime = Time.time;
        State = InteractState.Idle;
    }

    private Vector3 GetCurrentTargetPoint()
    {
        // 優先權 1：自動鎖定目標
        if (aimAssist && aimAssist.CurrentTarget)
        {
            return aimAssist.CurrentTarget.GetAimPoint();
        }

        // 優先權 2：從相機中心射出射線（最準確，無視角色朝向）
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimRaycastMask))
        {
            return hit.point;
        }
        
        return throwOrigin
            ? (throwOrigin.position + throwOrigin.forward * 20f)
            : (transform.position + transform.forward * 20f);
        // 優先權 3：相機前方固定距離點
        //return ray.GetPoint(20f); 
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

    private void OnAbsorbResult(Rigidbody rb, bool wasCollected, bool canThrow)
    {
        if (wasCollected)
        {
            State = InteractState.Idle;
            return;
        }

        if (rb && handSlot && handSlot.TryAttach(rb))
        {
            _canCurrentItemBeThrown = canThrow; // 紀錄此物件是否能被投擲
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