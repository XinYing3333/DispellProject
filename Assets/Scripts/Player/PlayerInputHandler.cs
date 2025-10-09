using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerInputHandler : MonoBehaviour, IPlayerInputSource
    {
        public static PlayerInputHandler Instance { get; private set; }
        public bool cannotMove;

        public Vector2 MoveInput { get; private set; }
        public float MoveSpeedMultiplier { get; private set; } = 1f;
        public bool JumpPressed { get; private set; }
        public bool SkillPressed { get; private set; }
        public bool DashPressed { get; private set; }
        public bool ShootPressed { get; private set; }
        public bool IsCollecting { get; private set; }
        public bool IsSkillUIOpen { get; private set; }
        public bool IsSettingPressed { get; private set; }
        public bool IsAiming { get; private set; }
        public bool InteractPressed => _interact.WasPressedThisFrame();

        public event Action OnJump;
        public event Action OnSkill;
        public event Action OnDash;
        public event Action OnSwitchThrow;

        [Header("Core Interaction")]
        [SerializeField] private InteractionController interaction;

        private PlayerInput _playerInput;
        private InputAction _movement, _run, _dash, _jump, _shoot, _collect, _interact, _aim ,_skill;
        private InputAction _skillUI, _setting;

        void Awake()
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
            _shoot    = _playerInput.actions["Shoot"];    // 投擲
            _collect  = _playerInput.actions["Collect"];  // 吸收（按住）
            _dash     = _playerInput.actions["Dash"];
            _interact = _playerInput.actions["Interact"]; // 丟下
            _aim      = _playerInput.actions["Aim"];
            _skill    = _playerInput.actions["Skill"];
            _skillUI  = _playerInput.actions["SkillUI"];
            _setting  = _playerInput.actions["Setting"];

            if (!interaction)
            {
                interaction = GetComponentInChildren<InteractionController>();
                if (!interaction)
                    Debug.LogWarning("[PlayerInputHandler] 尚未指定 InteractionController，吸收/投擲將無效。");
            }
        }

        private void OnEnable()
        {
            _collect.started   += OnCollectStarted;
            _collect.canceled  += OnCollectCanceled;

            _aim.started       += OnAimStarted;
            _aim.canceled      += OnAimCanceled;

            _jump.performed    += OnJumpPerformed;
            _dash.performed    += OnDashPerformed;
            _shoot.performed   += OnShootPerformed;   // 投擲
            _skill.performed   += OnSkillPerformed;
            _skillUI.performed += OnSkillUIPerformed;
            _setting.performed += OnSettingPerformed;
            _interact.performed+= OnInteractPerformed; // 丟下
        }

        private void OnDisable()
        {
            _collect.started   -= OnCollectStarted;
            _collect.canceled  -= OnCollectCanceled;

            _aim.started       -= OnAimStarted;
            _aim.canceled      -= OnAimCanceled;

            _jump.performed    -= OnJumpPerformed;
            _dash.performed    -= OnDashPerformed;
            _shoot.performed   -= OnShootPerformed;
            _skill.performed   -= OnSkillPerformed;
            _skillUI.performed -= OnSkillUIPerformed;
            _setting.performed -= OnSettingPerformed;
            _interact.performed-= OnInteractPerformed;
        }

        void Update()
        {
            if (cannotMove) return;

            MoveInput = _movement.ReadValue<Vector2>();
            string controlScheme = _playerInput.currentControlScheme;
            MoveSpeedMultiplier = (controlScheme == "Gamepad")
                ? Mathf.Clamp(MoveInput.magnitude, 0.1f, 1f)
                : (_run.ReadValue<float>() > 0.1f ? 0.5f : 1f);
        }

        public void ResetJump() => JumpPressed = false;
        public void ResetDash() => DashPressed = false;

        // ===== 吸收（按住） =====
        private void OnCollectStarted(InputAction.CallbackContext ctx)
        {
            IsCollecting = true;
            if (cannotMove || interaction == null) return;
            interaction.Input_StartAbsorbHold();
        }

        private void OnCollectCanceled(InputAction.CallbackContext ctx)
        {
            IsCollecting = false;
            if (cannotMove || interaction == null) return;
            // interaction.Input_StopAbsorbHold();
            interaction.Input_Drop();

        }

        // ===== 其他 =====
        private void OnAimStarted(InputAction.CallbackContext ctx)  => IsAiming = true;
        private void OnAimCanceled(InputAction.CallbackContext ctx) => IsAiming = false;

        private void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            JumpPressed = true;
            OnJump?.Invoke();
        }

        private void OnSkillPerformed(InputAction.CallbackContext ctx) => OnSkill?.Invoke();
        private void OnSkillUIPerformed(InputAction.CallbackContext ctx) => IsSkillUIOpen = !IsSkillUIOpen;
        private void OnSettingPerformed(InputAction.CallbackContext ctx) => IsSettingPressed = !IsSettingPressed;

        private void OnDashPerformed(InputAction.CallbackContext ctx)
        {
            DashPressed = true;
            OnDash?.Invoke();
        }

        // 投擲（有持有物才會成功；會自動瞄準，沒有就直前）
        private void OnShootPerformed(InputAction.CallbackContext ctx)
        {
            ShootPressed = true;
            if (cannotMove || interaction == null) return;
            interaction.Input_Throw();
            StartCoroutine(ClearShootFlagNextFrame());
        }

        // 丟下（不投擲）
        private void OnInteractPerformed(InputAction.CallbackContext ctx)
        {
            if (cannotMove || interaction == null) return;
            interaction.Input_Drop();
        }

        private IEnumerator ClearShootFlagNextFrame()
        {
            yield return null;
            ShootPressed = false;
        }
        public void SetSpellType(SpellType newSpellType)
        {
            // 若未使用可留空或保留原設計
            // spellPrefab.GetComponent<Spell>().spellType = newSpellType;
        }
    }
}
