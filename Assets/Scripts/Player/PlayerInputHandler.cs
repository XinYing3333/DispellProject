using System;
using System.Collections;
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

        private PlayerInput _playerInput;
        private InputAction _movement, _run, _dash, _jump, _shoot, _collect, _interact, _aim, _skill, _target;
        private InputAction _skillUI, _setting, _exit, _reset, _switch;

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
            _reset    = _playerInput.actions["Reset"];

            _aim      = _playerInput.actions["Aim"];
            _skill    = _playerInput.actions["Skill"];
            _switch   = _playerInput.actions["Switch"];
            _setting  = _playerInput.actions["Setting"];
            _target   = _playerInput.actions["Target"];

            if (!interaction)
            {
                interaction = GetComponentInChildren<InteractionController>();
                if (!interaction)
                    Debug.LogWarning("[PlayerInputHandler] 尚未指定 InteractionController，吸收/投擲將無效。");
            }
        }

        private void OnEnable()
        {
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
        }

        private void OnDisable()
        {
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
        }

        private void Update()
        {
            if (InputLock)
            {
                // 鎖定時不更新 MoveInput，確保角色不會被移動
                MoveInput = Vector2.zero;
                MoveSpeedMultiplier = 1f;
                return;
            }

            MoveInput = _movement.ReadValue<Vector2>();

            string controlScheme = _playerInput.currentControlScheme;
            MoveSpeedMultiplier = (controlScheme == "Gamepad")
                ? Mathf.Clamp(MoveInput.magnitude, 0.1f, 1f)
                : (_run.ReadValue<float>() > 0.1f ? 0.5f : 1f);
        }

        // ========= 只鎖「行動系統」的統一接口 =========
        public void SetLockMovement(bool lockMovement)
        {
            if (InputLock == lockMovement) return;
            InputLock = lockMovement;

            if (InputLock)
            {
                // 關掉進行中的 gameplay 行為（吸收、瞄準等）
                ForceStopContinuousGameplayStates();
                // 清除一次性 gameplay flag，避免解鎖後誤觸發
                ClearGameplayFlags();
            }
            // 解鎖時不用動 UI / 對話輸入，只是從下一幀開始重新讀取 Move / 行為
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
        }

        private void OnCollectCanceled(InputAction.CallbackContext ctx)
        {
            // 這裡即使鎖了也要確保能收尾，避免卡「吸收中」
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
    }
}
