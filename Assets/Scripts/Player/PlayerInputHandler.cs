using System;
using System.Collections;
using DefaultNamespace;
using EventBus.Events.UI;
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
        public bool IsSkiping { get; private set; }

        // 只在未鎖時允許切換（視為 gameplay）
        public bool SwitchPressed => !InputLock && _switch.WasPressedThisFrame();

        // ===== UI / System 用，不受 InputLock 影響 =====
        public bool InteractPressed => _interact.WasPressedThisFrame(); // 提供給對話 / UI 使用
        public bool ExitPressed => _exit.WasPressedThisFrame();
        public bool SelectPressed => _select.WasPressedThisFrame();
        public bool SettingRightPressed => _right.WasPressedThisFrame();

        // ★ 你原本的 Setting 改成 Pause
        public bool SettingLeftPressed => _left.WasPressedThisFrame();

        public event Action OnJump;
        public event Action OnSkill;
        public event Action OnDash;
        public event Action OnSwitchThrow;

        [Header("Core Interaction")]
        [SerializeField] private InteractionController interaction;

        [Header("Action Map Names")]
        [SerializeField] private string gameplayMapName = "Gameplay";
        [SerializeField] private string uiMapName = "UI";

        private PlayerInput _playerInput;

        // ===== Gameplay actions =====
        private InputAction _movement, _run, _dash, _jump, _shoot, _collect, _interact, _aim, _skill, _target;
        private InputAction _exit, _left, _right, _switch, _select;

        // ===== UI/System actions =====
        private InputAction _pause; // ★ 由原本 Setting 改名

        // ===== Pause state =====
        public bool IsPaused{ get; private set; }

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
            _select     = _playerInput.actions["Select"];
            _left    = _playerInput.actions["SettingLeft"];
            _right    = _playerInput.actions["SettingRight"];

            _aim      = _playerInput.actions["Aim"];
            _skill    = _playerInput.actions["Skill"];
            _switch   = _playerInput.actions["Switch"];
            _target   = _playerInput.actions["Target"];

            
            _pause = _playerInput.actions["Pause"];

            if (!interaction)
            {
                interaction = GetComponentInChildren<InteractionController>();
                if (!interaction)
                    Debug.LogWarning("[PlayerInputHandler] 尚未指定 InteractionController，吸收/投擲將無效。");
            }
        }

        private void OnEnable()
        {
            // ===== Gameplay subscribes =====
            _collect.started  += OnCollectStarted;
            _collect.canceled += OnCollectCanceled;

            _aim.started      += OnAimStarted;
            _aim.canceled     += OnAimCanceled;
            
            _interact.started  += OnSkipStarted;
            _interact.canceled += OnSkipCanceled;

            _jump.performed     += OnJumpPerformed;
            _dash.performed     += OnDashPerformed;
            _shoot.performed    += OnShootPerformed;
            _skill.performed    += OnSkillPerformed;
            _interact.performed += OnInteractPerformed;
            _target.performed   += OnTargetPerformed;

            // ===== Pause subscribe（不受 InputLock）=====
            if (_pause != null) _pause.performed += OnPausePerformed;
        }

        private void OnDisable()
        {
            // ===== Gameplay unsubscribes =====
            _collect.started  -= OnCollectStarted;
            _collect.canceled -= OnCollectCanceled;

            _aim.started      -= OnAimStarted;
            _aim.canceled     -= OnAimCanceled;

            _interact.started  -= OnSkipStarted;
            _interact.canceled -= OnSkipCanceled;
            
            _jump.performed     -= OnJumpPerformed;
            _dash.performed     -= OnDashPerformed;
            _shoot.performed    -= OnShootPerformed;
            _skill.performed    -= OnSkillPerformed;
            _interact.performed -= OnInteractPerformed;
            _target.performed   -= OnTargetPerformed;

            // ===== Pause unsubscribe =====
            if (_pause != null) _pause.performed -= OnPausePerformed;

            StopHoldRumble();
        }

        private void Update()
        {
            // ===== 移動輸入更新（受 InputLock 影響）=====
            if (InputLock)
            {
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

            // ===== 連續震動維持 =====
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

        // ========= Pause toggle（本腳本內部處理） =========
        private void OnPausePerformed(InputAction.CallbackContext ctx)
        {
            TogglePause();
        }

        public void TogglePause()
        {
            SetPauseMode(!IsPaused);
        }

        public void SetPauseMode(bool paused)
        {
            IsPaused = paused;

            EventBus<OnTogglePause>.Raise(new OnTogglePause());

            SetLockMovement(paused);

            Time.timeScale = paused ? 0f : 1f;
        }

        private void SwitchActionMap(string mapName)
        {
            if (_playerInput == null) return;
            if (string.IsNullOrEmpty(mapName)) return;

            if (_playerInput.currentActionMap != null && _playerInput.currentActionMap.name == mapName)
                return;

            try { _playerInput.SwitchCurrentActionMap(mapName); }
            catch { /* map 名稱不對就不炸 */ }
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

        // ========= Gameplay callbacks（受 InputLock 影響） =========
        private void OnCollectStarted(InputAction.CallbackContext ctx)
        {
            if (InputLock || interaction == null) return;

            IsCollecting = true;
            interaction.Input_StartAbsorbHold();

            StartHoldRumble(0.15f, 0.30f);
        }

        private void OnCollectCanceled(InputAction.CallbackContext ctx)
        {
            StopHoldRumble();

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
        private void OnSkipStarted(InputAction.CallbackContext ctx)
        {
            IsSkiping = true;
        }
        private void OnSkipCanceled(InputAction.CallbackContext ctx)
        {
            IsSkiping = false;
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
        /// 只處理 Gameplay 的丟下物件版本。對話/UI 請讀 InteractPressed（不受 InputLock 影響）
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
                if (Gamepad.current != null)
                    Gamepad.current.SetMotorSpeeds(_rumbleLow, _rumbleHigh);
            }
            else
            {
                if (Gamepad.current != null)
                    Gamepad.current.SetMotorSpeeds(0f, 0f);
            }
        }
    }
}
