using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Player
{
    public class PlayerInputHandler : MonoBehaviour, IPlayerInputSource
    {
        // === Input Properties ===
        public Vector2 MoveInput { get; private set; }
        public float MoveSpeedMultiplier { get; private set; } = 1f;
        public bool JumpPressed { get; private set; }
        public bool DashPressed { get; private set; }
        public bool IsCollecting { get; private set; }
        public bool IsAiming { get; private set; }
        public bool InteractPressed => _interact.WasPressedThisFrame();
        public ThrowType CurrentThrowType { get; private set; } = ThrowType.Spells;


        // === Events ===
        public event Action OnJump;
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
        public Image switchImage;

        // === Private ===
        private PlayerInput _playerInput;
        private InputAction _movement, _run, _dash, _jump, _switch, _shoot, _collect, _interact, _aim;
        private Camera _playerCamera;
        private PlayerCollector _playerCollector;
        private ThrowingSystem _throwingSystem;

        void Awake()
        {
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
            _switch = _playerInput.actions["Switch"];
            _shoot = _playerInput.actions["Shoot"];
            _collect = _playerInput.actions["Collect"];
            _dash = _playerInput.actions["Dash"];
            _interact = _playerInput.actions["Interact"];
            _aim = _playerInput.actions["Aim"];
        }

        private void OnEnable()
        {
            /*_collect.started += OnCollectStarted;
            _collect.canceled += OnCollectCanceled;*/
            
            _collect.performed += OnCollectPerformed;

            _aim.started += OnAimStarted;
            _aim.canceled += OnAimCanceled;

            _jump.performed += OnJumpPerformed;
            _dash.performed += OnDashPerformed;
            _shoot.performed += OnShootPerformed;
            _switch.performed += OnSwitchPerformed;
        }

        private void OnDisable()
        {
            /*_collect.started -= OnCollectStarted;
            _collect.canceled -= OnCollectCanceled;*/
            
            _collect.performed -= OnCollectPerformed;

            _aim.started -= OnAimStarted;
            _aim.canceled -= OnAimCanceled;

            _jump.performed -= OnJumpPerformed;
            _dash.performed -= OnDashPerformed;
            _shoot.performed -= OnShootPerformed;
            _switch.performed -= OnSwitchPerformed;
            
            _collect.performed += OnCollectPerformed;

        }

        void Update()
        {
            MoveInput = _movement.ReadValue<Vector2>();

            string controlScheme = _playerInput.currentControlScheme;
            MoveSpeedMultiplier = (controlScheme == "Gamepad")
                ? Mathf.Clamp(MoveInput.magnitude, 0.1f, 1f)
                : (_run.ReadValue<float>() > 0.1f ? 0.5f : 1f);

            /*if (IsCollecting)
                _playerCollector.OnCollectCollectibles();*/
        }

        public void ResetJump() => JumpPressed = false;
        public void ResetDash() => DashPressed = false;

        // ==== Input Callbacks ====
        private void OnCollectStarted(InputAction.CallbackContext ctx) => IsCollecting = true;
        private void OnCollectCanceled(InputAction.CallbackContext ctx) => IsCollecting = false;

        private void OnAimStarted(InputAction.CallbackContext ctx) => IsAiming = true;
        private void OnAimCanceled(InputAction.CallbackContext ctx) => IsAiming = false;

        private void OnCollectPerformed(InputAction.CallbackContext ctx)
        {
            _playerCollector.OnCollectCollectibles();
        }

        
        private void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            JumpPressed = true;
            OnJump?.Invoke();
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

        private void OnSwitchPerformed(InputAction.CallbackContext ctx)
        {
            HandleThrowSwitch();
            OnSwitchThrow?.Invoke();
        }

        // ==== Internal Logic ====
        private void HandleThrowSwitch()
        {
            CurrentThrowType = (CurrentThrowType == ThrowType.Spells) ? ThrowType.ThrowableObjects : ThrowType.Spells;

            if (switchImage != null)
                switchImage.color = (CurrentThrowType == ThrowType.ThrowableObjects) ? Color.white : Color.black;
        }

        private void HandleShoot()
        {
            AudioManager.Instance.PlaySFX(SFXType.Shoot);

            if (CurrentThrowType == ThrowType.ThrowableObjects)
            {
                if (CollectionSystem.GetDictionaryCount() > 0)
                {
                    _throwingSystem.ThrowObject(CurrentThrowType, _playerCamera);
                    CollectionSystem.UseItem();
                }
                else
                {
                    Debug.Log("沒有可用的投擲物");
                }
            }
            else if (CurrentThrowType == ThrowType.Spells)
            {
                _throwingSystem.ThrowObject(CurrentThrowType, _playerCamera);
            }
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
