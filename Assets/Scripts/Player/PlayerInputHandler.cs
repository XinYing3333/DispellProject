using System;
using System.Collections;
using UI.Pause;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerInputHandler : MonoBehaviour, IPlayerInputSource
    {
        public static PlayerInputHandler Instance { get; private set; }

        /// <summary>
        /// 僅代表「角色行動相關」是否被鎖，UI / 對話不受影響
        /// </summary>
        public bool InputLock { get; private set; }

        public Vector2 MoveInput { get; private set; }
        public float MoveSpeedMultiplier { get; private set; } = 1f;

        // ===== Gameplay flags =====
        public bool JumpPressed { get; private set; }
        public bool SkillPressed { get; private set; }
        public bool DashPressed { get; private set; }
        public bool ShootPressed { get; private set; }
        public bool IsCollecting { get; private set; }
        public bool IsTargetPressed { get; private set; }
        public bool IsAiming { get; private set; }

        // 只在未鎖時允許切換（視為 gameplay）
        public bool SwitchPressed => !InputLock && _switch.WasPressedThisFrame();

        // ===== UI / System 用，不受 InputLock 影響 =====
        public bool IsSkillUIOpen { get; private set; } // 你之後如果要用再補
        public bool IsSettingPressed => _setting.WasPressedThisFrame();
        public bool InteractPressed => _interact.WasPressedThisFrame();   // 提供給對話 / UI 使用
        public bool ExitPressed => _exit.WasPressedThisFrame();
        public bool ResetPressed => _reset.WasPressedThisFrame();

        public event Action OnJump;
        public event Action OnSkill;
        public event Action OnDash;
        public event Action OnSwitchThrow;

        [Header("Core Interaction")]
        [SerializeField] private InteractionController interaction;

        [Header("Pause")]
        [SerializeField] private PauseController pauseController;

        // 你的 PlayerInput 可能已經在用多個 Action Map
        // 這裡只做「一致化」：Pause 開啟 -> 切到 UI Map；Pause 關閉 -> 回到 Gameplay Map
        // 若你的 Map 名稱不同，直接改字串即可（不需要新增資產）
        [SerializeField] private string gameplayMapName = "Gameplay";
        [SerializeField] private string uiMapName = "UI";

        private PlayerInput _playerInput;
        private InputAction _movement, _run, _dash, _jump, _shoot, _collect, _interact, _aim, _skill, _target;
        private InputAction _skillUI, _setting, _exit, _reset, _switch;

        // ===== Pause / UI actions（不受 InputLock 影響）=====
        private InputAction _pauseToggle, _submit, _cancel, _tabLeft, _tabRight, _navigate;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogError("PlayerInput 未正確掛載");
                return;
            }

            // ===== Gameplay =====
            _movement = _playerInput.actions["Move"];
            _run      = _playerInput.actions["Run"];
            _jump     = _playerInput.actions["Jump"];
            _shoot    = _playerInput.actions["Shoot"];
            _collect  = _playerInput.actions["Collect"];
            _dash     = _playerInput.actions["Dash"];
            _interact = _playerInput.actions["Interact"];

            _exit     = _playerInput.actions["Exit"];
            _reset    = _playerInput.actions["Reset"];

            _aim      = _playerInput.actions["Aim"];
            _skill    = _playerInput.actions["Skill"];
            _switch   = _playerInput.actions["Switch"];
            _setting  = _playerInput.actions["Setting"];
            _target   = _playerInput.actions["Target"];

            // ===== Pause/UI（若你 action 名稱不同，改這裡的 key）=====
            // 允許不存在：你還沒建 action 時不會炸，只是不支援 Pause。
            TryGetAction("PauseToggle", out _pauseToggle);
            TryGetAction("Submit", out _submit);
            TryGetAction("Cancel", out _cancel);
            TryGetAction("TabLeft", out _tabLeft);
            TryGetAction("TabRight", out _tabRight);
            TryGetAction("Navigate", out _navigate);

            if (!interaction)
            {
                interaction = GetComponentInChildren<InteractionController>();
                if (!interaction)
                    Debug.LogWarning("[PlayerInputHandler] 尚未指定 InteractionController，吸收/投擲將無效。");
            }

            if (!pauseController)
                pauseController = FindFirstObjectByType<PauseController>();
        }

        private void OnEnable()
        {
            // ===== Gameplay subscribes =====
            _collect.started  += OnCollectStarted;
            _collect.canceled += OnCollectCanceled;

            _aim.started      += OnAimStarted;
            _aim.canceled     += OnAimCanceled;

            _jump.performed   += OnJumpPerformed;
            _dash.performed   += OnDashPerformed;
            _shoot.performed  += OnShootPerformed;
            _skill.performed  += OnSkillPerformed;
            _interact.performed += OnInteractPerformed;
            _target.performed += OnTargetPerformed;

            // ===== Pause / UI subscribes（不受 InputLock）=====
            if (_pauseToggle != null) _pauseToggle.performed += OnPauseTogglePerformed;
            if (_submit != null)      _submit.performed      += OnSubmitPerformed;
            if (_cancel != null)      _cancel.performed      += OnCancelPerformed;
            if (_tabLeft != null)     _tabLeft.performed     += OnTabLeftPerformed;
            if (_tabRight != null)    _tabRight.performed    += OnTabRightPerformed;

            // Navigate：鍵盤 2D Vector / 手把搖桿都會進來（performed + canceled）
            if (_navigate != null)
            {
                _navigate.performed += OnNavigatePerformed;
                _navigate.canceled  += OnNavigateCanceled;
            }
        }

        private void OnDisable()
        {
            // ===== Gameplay unsubscribes =====
            _collect.started  -= OnCollectStarted;
            _collect.canceled -= OnCollectCanceled;

            _aim.started      -= OnAimStarted;
            _aim.canceled     -= OnAimCanceled;

            _jump.performed   -= OnJumpPerformed;
            _dash.performed   -= OnDashPerformed;
            _shoot.performed  -= OnShootPerformed;
            _skill.performed  -= OnSkillPerformed;
            _interact.performed -= OnInteractPerformed;
            _target.performed -= OnTargetPerformed;

            // ===== Pause / UI unsubscribes =====
            if (_pauseToggle != null) _pauseToggle.performed -= OnPauseTogglePerformed;
            if (_submit != null)      _submit.performed      -= OnSubmitPerformed;
            if (_cancel != null)      _cancel.performed      -= OnCancelPerformed;
            if (_tabLeft != null)     _tabLeft.performed     -= OnTabLeftPerformed;
            if (_tabRight != null)    _tabRight.performed    -= OnTabRightPerformed;

            if (_navigate != null)
            {
                _navigate.performed -= OnNavigatePerformed;
                _navigate.canceled  -= OnNavigateCanceled;
            }

            StopHoldRumble();
        }

        private void Update()
        {
            if (InputLock)
            {
                // 鎖定時不更新 MoveInput，確保角色不會被移動
                MoveInput = Vector2.zero;
                MoveSpeedMultiplier = 1f;
            }
            else
            {
                MoveInput = _movement.ReadValue<Vector2>();

                string controlScheme = _playerInput.currentControlScheme;
                MoveSpeedMultiplier = (controlScheme == "Gamepad")
                    ? Mathf.Clamp(MoveInput.magnitude, 0.1f, 1f)
                    : (_run.ReadValue<float>() > 0.1f ? 0.5f : 1f);
            }

            // 連續震動維持（只在手把存在且 scheme 正確時）
            if (_isRumbling)
            {
                if (_playerInput.currentControlScheme != "Gamepad" || Gamepad.current == null)
                {
                    StopHoldRumble();
                }
                else
                {
                    Gamepad.current.SetMotorSpeeds(_rumbleLow, _rumbleHigh);
                }
            }
        }

        // ========= 只鎖「行動系統」的統一接口 =========
        public void SetLockMovement(bool lockMovement)
        {
            if (InputLock == lockMovement) return;
            InputLock = lockMovement;

            if (Gamepad.current != null)
                Gamepad.current.SetMotorSpeeds(0f, 0f);

            if (InputLock)
            {
                StopHoldRumble();
                ForceStopContinuousGameplayStates();
                ClearGameplayFlags();
            }
        }

        // ========= Pause: 由 PauseController 呼叫（或你自行在其它系統呼叫） =========
        public void SetPauseMode(bool paused)
        {
            SetLockMovement(paused);
            SwitchActionMap(paused ? uiMapName : gameplayMapName);
        }

        private void SwitchActionMap(string mapName)
        {
            if (_playerInput == null) return;
            if (string.IsNullOrEmpty(mapName)) return;

            // 避免重複切換造成 GC/狀態抖動
            if (_playerInput.currentActionMap != null && _playerInput.currentActionMap.name == mapName)
                return;

            try
            {
                _playerInput.SwitchCurrentActionMap(mapName);
            }
            catch
            {
                // Map 名稱不對時，不炸；你要改 gameplayMapName/uiMapName
            }
        }

        public void ResetJump() => JumpPressed = false;
        public void ResetDash() => DashPressed = false;

        // ========= 內部工具 =========
        private void ForceStopContinuousGameplayStates()
        {
            if (IsCollecting && interaction != null)
                interaction.Input_Drop();

            IsCollecting = false;
            IsAiming = false;
        }

        private void ClearGameplayFlags()
        {
            MoveInput = Vector2.zero;
            MoveSpeedMultiplier = 1f;

            JumpPressed = false;
            DashPressed = false;
            ShootPressed = false;
            SkillPressed = false;
            IsTargetPressed = false;
        }

        // ========= Callback（僅 gameplay 才看 InputLock） =========
        private void OnCollectStarted(InputAction.CallbackContext ctx)
        {
            if (InputLock || interaction == null) return;

            IsCollecting = true;
            interaction.Input_StartAbsorbHold();

            // 按住期間持續震動
            StartHoldRumble(0.15f, 0.30f);
        }

        private void OnCollectCanceled(InputAction.CallbackContext ctx)
        {
            StopHoldRumble();

            // 即使鎖了也要收尾，避免卡「吸收中」
            IsCollecting = false;
            if (interaction != null)
                interaction.Input_Drop();
        }

        private void OnAimStarted(InputAction.CallbackContext ctx)
        {
            if (InputLock) return;
            IsAiming = true;
        }

        private void OnAimCanceled(InputAction.CallbackContext ctx)
        {
            IsAiming = false;
        }

        private void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            if (InputLock) return;
            JumpPressed = true;
            OnJump?.Invoke();
        }

        private void OnSkillPerformed(InputAction.CallbackContext ctx)
        {
            if (InputLock) return;
            SkillPressed = true;
            OnSkill?.Invoke();
        }

        private void OnDashPerformed(InputAction.CallbackContext ctx)
        {
            if (InputLock) return;
            DashPressed = true;
            OnDash?.Invoke();
        }

        private void OnShootPerformed(InputAction.CallbackContext ctx)
        {
            if (InputLock || interaction == null) return;

            ShootPressed = true;
            interaction.Input_Throw();
            StartCoroutine(ClearShootFlagNextFrame());

            Rumble(0.3f, 0.7f, 0.1f);
        }

        /// <summary>
        /// 注意：這裡只處理「丟下物件」的 Gameplay 版本。
        /// 對話 / UI 請讀取 InteractPressed 屬性自己用，不會被 InputLock 擋。
        /// </summary>
        private void OnInteractPerformed(InputAction.CallbackContext ctx)
        {
            if (InputLock || interaction == null) return;
            interaction.Input_Drop();
        }

        private void OnTargetPerformed(InputAction.CallbackContext ctx)
        {
            if (InputLock) return;
            IsTargetPressed = !IsTargetPressed;
        }

        private IEnumerator ClearShootFlagNextFrame()
        {
            yield return null;
            ShootPressed = false;
        }

        public void SetSpellType(SpellType newSpellType)
        {
            //
        }

        // ========= Pause/UI callbacks（不受 InputLock） =========

        private void OnPauseTogglePerformed(InputAction.CallbackContext ctx)
        {
            if (pauseController == null) return;

            // 直接交給 PauseController 做狀態機；InputHandler 不做判斷
            pauseController.CmdPauseToggle();
        }

        private void OnSubmitPerformed(InputAction.CallbackContext ctx)
        {
            if (pauseController == null) return;
            pauseController.CmdSubmit();
        }

        private void OnCancelPerformed(InputAction.CallbackContext ctx)
        {
            if (pauseController == null) return;
            pauseController.CmdCancel();
        }

        private void OnTabLeftPerformed(InputAction.CallbackContext ctx)
        {
            if (pauseController == null) return;
            pauseController.CmdTabLeft();
        }

        private void OnTabRightPerformed(InputAction.CallbackContext ctx)
        {
            if (pauseController == null) return;
            pauseController.CmdTabRight();
        }

        // 導航 Gate：避免搖桿持續輸出導致每幀觸發
        private Vector2 _lastNav;
        [SerializeField] private float navigateDeadzone = 0.5f;

        private void OnNavigatePerformed(InputAction.CallbackContext ctx)
        {
            if (pauseController == null) return;

            var v = ctx.ReadValue<Vector2>();
            Vector2 snapped = Vector2.zero;

            if (v.x <= -navigateDeadzone) snapped.x = -1f;
            else if (v.x >= navigateDeadzone) snapped.x = 1f;

            if (v.y <= -navigateDeadzone) snapped.y = -1f;
            else if (v.y >= navigateDeadzone) snapped.y = 1f;

            if (snapped == Vector2.zero) return;

            if (snapped != _lastNav)
            {
                _lastNav = snapped;
                pauseController.CmdNavigate(snapped);
            }
        }

        private void OnNavigateCanceled(InputAction.CallbackContext ctx)
        {
            _lastNav = Vector2.zero;
        }

        // ========= Rumble =========
        private bool _isRumbling;
        private float _rumbleLow;
        private float _rumbleHigh;

        private void StartHoldRumble(float low, float high)
        {
            if (_playerInput.currentControlScheme != "Gamepad") return;
            var pad = Gamepad.current;
            if (pad == null) return;

            _rumbleLow = low;
            _rumbleHigh = high;
            _isRumbling = true;

            pad.SetMotorSpeeds(_rumbleLow, _rumbleHigh);
        }

        private void StopHoldRumble()
        {
            _isRumbling = false;

            var pad = Gamepad.current;
            if (pad != null)
                pad.SetMotorSpeeds(0f, 0f);
        }

        private void Rumble(float low, float high, float duration)
        {
            if (_playerInput.currentControlScheme != "Gamepad") return;

            var pad = Gamepad.current;
            if (pad == null) return;

            pad.SetMotorSpeeds(low, high);
            StartCoroutine(StopRumbleAfter(duration));
        }

        private IEnumerator StopRumbleAfter(float t)
        {
            yield return new WaitForSecondsRealtime(t);

            if (_isRumbling)
            {
                // 若此時仍在 HoldRumble，恢復持續震動
                if (Gamepad.current != null)
                    Gamepad.current.SetMotorSpeeds(_rumbleLow, _rumbleHigh);
            }
            else
            {
                if (Gamepad.current != null)
                    Gamepad.current.SetMotorSpeeds(0f, 0f);
            }
        }

        // ========= Helpers =========
        private void TryGetAction(string actionName, out InputAction action)
        {
            action = null;
            if (_playerInput == null || _playerInput.actions == null) return;
            try
            {
                action = _playerInput.actions[actionName];
            }
            catch
            {
                action = null;
            }
        }
    }
}
