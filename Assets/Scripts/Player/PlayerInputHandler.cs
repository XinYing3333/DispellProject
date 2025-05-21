using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Player
{
    public class PlayerInputHandler : MonoBehaviour, IPlayerInputSource
    {
        public static PlayerInputHandler Instance { get; private set; }
        
        // === Input Properties ===
        public Vector2 MoveInput { get; private set; }
        public float MoveSpeedMultiplier { get; private set; } = 1f;
        public bool JumpPressed { get; private set; }
        public bool SkillPressed { get; private set; }
        public bool DashPressed { get; private set; }
        public bool IsCollecting { get; private set; }
        public bool IsSkillUIOpen { get; private set; }

        public bool IsAiming { get; private set; }
        public bool InteractPressed => _interact.WasPressedThisFrame();


        // === Events ===
        public event Action OnJump;
        public event Action OnSkill;
        public event Action OnDash;
        public event Action OnShoot;
        public event Action OnSwitchThrow;

        // === Dependencies ===
        [Header("Throwing System")]
        public GameObject throwablePrefab;
        public GameObject spellPrefab;
        public Transform throwPoint;
        public float throwForce = 10f;
        public Transform checkPoint;

        // === Private ===
        private PlayerInput _playerInput;
        private InputAction _movement, _run, _dash, _jump, _shoot, _collect, _interact, _aim ,_skill;
        private InputAction _skillUI, _setting;
        private Camera _playerCamera;
        private PlayerCollector _playerCollector;
        private ThrowingSystem _throwingSystem;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            checkPoint = GameObject.FindGameObjectWithTag("CheckPoint").transform;
            _playerInput = GetComponent<PlayerInput>();
            _playerCollector = GetComponent<PlayerCollector>();
            _playerCamera = Camera.main;
            _throwingSystem = new ThrowingSystem(throwablePrefab, spellPrefab, throwPoint, throwForce);

            if (_playerInput == null)
            {
                Debug.LogError("PlayerInput 未正確掛載");
                return;
            }

            // Assign actions
            _movement = _playerInput.actions["Move"];
            _run = _playerInput.actions["Run"];
            _jump = _playerInput.actions["Jump"];
            _shoot = _playerInput.actions["Shoot"];
            _collect = _playerInput.actions["Collect"];
            _dash = _playerInput.actions["Dash"];
            _interact = _playerInput.actions["Interact"];
            _aim = _playerInput.actions["Aim"];
            _skill = _playerInput.actions["Skill"];
            _skillUI = _playerInput.actions["SkillUI"];

        }

        private void OnEnable()
        {
            _collect.started += OnCollectStarted;
            _collect.canceled += OnCollectCanceled;

            _aim.started += OnAimStarted;
            _aim.canceled += OnAimCanceled;

            _jump.performed += OnJumpPerformed;
            _dash.performed += OnDashPerformed;
            _shoot.performed += OnShootPerformed;
            _skill.performed += OnSkillPerformed;
            _skillUI.performed += OnSkillUIPerformed;
            
           // _switch.performed += OnSwitchPerformed;
        }

        private void OnDisable()
        {
            _collect.started -= OnCollectStarted;
            _collect.canceled -= OnCollectCanceled;

            _aim.started -= OnAimStarted;
            _aim.canceled -= OnAimCanceled;

            _jump.performed -= OnJumpPerformed;
            _dash.performed -= OnDashPerformed;
            _shoot.performed -= OnShootPerformed;
            _skill.performed -= OnSkillPerformed;
            _skillUI.performed -= OnSkillUIPerformed;
            
            //_switch.performed -= OnSwitchPerformed;
        }

        void Update()
        {
            MoveInput = _movement.ReadValue<Vector2>();

            string controlScheme = _playerInput.currentControlScheme;
            MoveSpeedMultiplier = (controlScheme == "Gamepad")
                ? Mathf.Clamp(MoveInput.magnitude, 0.1f, 1f)
                : (_run.ReadValue<float>() > 0.1f ? 0.5f : 1f);

            if (IsCollecting)
            {
                _playerCollector.OnCollectCollectibles();
            }
            else
            {
                _playerCollector.OnCancelCollect();   
            }
        }

        public void ResetJump() => JumpPressed = false;
        public void ResetDash() => DashPressed = false;

        // ==== Input Callbacks ====
        private void OnCollectStarted(InputAction.CallbackContext ctx) => IsCollecting = true;
        private void OnCollectCanceled(InputAction.CallbackContext ctx) => IsCollecting = false;

        private void OnAimStarted(InputAction.CallbackContext ctx) => IsAiming = true;
        private void OnAimCanceled(InputAction.CallbackContext ctx) => IsAiming = false;

        private void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            JumpPressed = true;
            OnJump?.Invoke();
        }
        
        private void OnSkillPerformed(InputAction.CallbackContext ctx)
        {
            OnSkill?.Invoke();
        }
        
        private void OnSkillUIPerformed(InputAction.CallbackContext ctx)
        {
            IsSkillUIOpen = !IsSkillUIOpen;
        }

        private void OnDashPerformed(InputAction.CallbackContext ctx)
        {
            DashPressed = true;
            OnDash?.Invoke();
        }

        private void OnShootPerformed(InputAction.CallbackContext ctx)
        {
            OnShoot?.Invoke();
            HandleShoot();
        }
        
        
        /*private void OnSwitchPerformed(InputAction.CallbackContext ctx)
        {
            HandleThrowSwitch();
            OnSwitchThrow?.Invoke();
        }*/

        // ==== Internal Logic ====

        private void HandleShoot()
        {
            AudioManager.Instance.PlaySFX(SFXType.Shoot);
            
            _throwingSystem.ThrowObject(transform);
        }

        public void SetSpellType(SpellType newSpellType)
        {
            spellPrefab.GetComponent<Spell>().spellType = newSpellType;
        }
    
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("DeadPoint"))
            {
                transform.position = checkPoint.position;
            }
        }
    }
}
