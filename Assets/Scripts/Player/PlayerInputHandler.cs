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
        public bool IsSkipping { get; private set; } 

        // ===== UI / System Flags =====
        public bool SwitchPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool ExitPressed { get; private set; }
        public bool SelectPressed { get; private set; }
        public bool SettingRightPressed { get; private set; }
        public bool SettingLeftPressed { get; private set; }

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

        // ===== Actions =====
        private InputAction _movement, _run, _dash, _jump, _shoot, _collect, _interact, _aim, _skill, _target;
        private InputAction _exit, _left, _right, _switch, _select, _pause;

        public bool IsPaused { get; private set; }

        // ===== Cache =====
        private bool _isGamepadControl;

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

            _movement = _playerInput.actions["Move"];
            _run      = _playerInput.actions["Run"];
            _jump     = _playerInput.actions["Jump"];
            _shoot    = _playerInput.actions["Shoot"];
            _collect  = _playerInput.actions["Collect"];
            _dash     = _playerInput.actions["Dash"];
            _interact = _playerInput.actions["Interact"];

            _exit     = _playerInput.actions["Exit"];
            _select   = _playerInput.actions["Select"];
            _left     = _playerInput.actions["SettingLeft"];
            _right    = _playerInput.actions["SettingRight"];

            _aim      = _playerInput.actions["Aim"];
            _skill    = _playerInput.actions["Skill"];
            _switch   = _playerInput.actions["Switch"];
            _target   = _playerInput.actions["Target"];
            
            _pause    = _playerInput.actions["Pause"];

            if (!interaction)
            {
                interaction = GetComponentInChildren<InteractionController>();
                if (!interaction)
                    Debug.LogWarning("[PlayerInputHandler] 尚未指定 InteractionController，吸收/投擲將無效。");
            }
        }

        private void OnEnable()
        {
            BindEvents(true);
            _playerInput.onControlsChanged += OnControlsChanged;
            UpdateControlSchemeCache();
        }

        private void OnDisable()
        {
            BindEvents(false);
            _playerInput.onControlsChanged -= OnControlsChanged;
            StopHoldRumble();
        }

        // 統一事件綁定管理
        private void BindEvents(bool bind)
        {
            if (bind)
            {
                _collect.started  += OnCollectStarted;
                _collect.canceled += OnCollectCanceled;
                _aim.started      += OnAimStarted;
                _aim.canceled     += OnAimCanceled;
                _interact.started += OnSkipStarted;
                _interact.canceled += OnSkipCanceled;

                _jump.performed     += OnJumpPerformed;
                _dash.performed     += OnDashPerformed;
                _shoot.performed    += OnShootPerformed;
                _skill.performed    += OnSkillPerformed;
                _interact.performed += OnInteractPerformed;
                _target.performed   += OnTargetPerformed;

                if (_pause != null) _pause.performed += OnPausePerformed;
            }
            else
            {
                _collect.started  -= OnCollectStarted;
                _collect.canceled -= OnCollectCanceled;
                _aim.started      -= OnAimStarted;
                _aim.canceled     -= OnAimCanceled;
                _interact.started -= OnSkipStarted;
                _interact.canceled -= OnSkipCanceled;
                
                _jump.performed     -= OnJumpPerformed;
                _dash.performed     -= OnDashPerformed;
                _shoot.performed    -= OnShootPerformed;
                _skill.performed    -= OnSkillPerformed;
                _interact.performed -= OnInteractPerformed;
                _target.performed   -= OnTargetPerformed;

                if (_pause != null) _pause.performed -= OnPausePerformed;
            }
        }

        private void OnControlsChanged(PlayerInput input)
        {
            UpdateControlSchemeCache();
        }

        private void UpdateControlSchemeCache()
        {
            _isGamepadControl = _playerInput.currentControlScheme == "Gamepad";
        }

        private void Update()
        {
            SwitchPressed = !InputLock && _switch.WasPressedThisFrame();
            InteractPressed = _interact.WasPressedThisFrame();
            ExitPressed = _exit.WasPressedThisFrame();
            SelectPressed = _select.WasPressedThisFrame();
            SettingRightPressed = _right.WasPressedThisFrame();
            SettingLeftPressed = _left.WasPressedThisFrame();

            if (InputLock)
            {
                MoveInput = Vector2.zero;
                MoveSpeedMultiplier = 1f;
            }
            else
            {
                MoveInput = _movement.ReadValue<Vector2>();

                MoveSpeedMultiplier = _isGamepadControl
                    ? Mathf.Clamp(MoveInput.magnitude, 0.1f, 1f)
                    : (_run.ReadValue<float>() > 0.1f ? 0.5f : 1f);
            }

            if (_isRumbling)
            {
                if (!_isGamepadControl || Gamepad.current == null)
                {
                    StopHoldRumble();
                }
                else
                {
                    Gamepad.current.SetMotorSpeeds(_rumbleLow, _rumbleHigh);
                }
            }
        }

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

        public void SetLockMovement(bool lockMovement)
        {
            if (InputLock == lockMovement) return;
            InputLock = lockMovement;

            if (Gamepad.current != null)
                Gamepad.current.SetMotorSpeeds(0f, 0f);

            if (InputLock)
            {
                StopHoldRumble();
                ClearAllGameplayStates();
            }
        }

        public void ResetJump() => JumpPressed = false;
        public void ResetDash() => DashPressed = false;
        
        private void ClearAllGameplayStates()
        {
            if (IsCollecting && interaction != null)
                interaction.Input_Drop();

            IsCollecting = false;
            IsAiming = false;
            
            MoveInput = Vector2.zero;
            MoveSpeedMultiplier = 1f;
            JumpPressed = false;
            DashPressed = false;
            ShootPressed = false;
            SkillPressed = false;
            IsTargetPressed = false;
        }

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

        private void OnAimCanceled(InputAction.CallbackContext ctx) => IsAiming = false;

        private void OnSkipStarted(InputAction.CallbackContext ctx) => IsSkipping = true;

        private void OnSkipCanceled(InputAction.CallbackContext ctx) => IsSkipping = false;

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
        

        // ========= Rumble =========
        private bool _isRumbling;
        private float _rumbleLow;
        private float _rumbleHigh;

        private void StartHoldRumble(float low, float high)
        {
            if (!_isGamepadControl) return;
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
            if (!_isGamepadControl) return;

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