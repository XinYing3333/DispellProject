using System.Collections;
using Player;
using UnityEngine;

namespace AbilitySystem
{
    public class CloneMovement : MonoBehaviour
    {
        public Animator anim;
        public Transform cameraTransform;

        private PlayerInputHandler input;

        private Rigidbody _rb;

        [Header("Movement Settings")] 
        [SerializeField] private float movementSpeed = 2f;
        [SerializeField] private float runSpeed = 4f;
        [SerializeField] private float turnSpeed = 10f;
        
        private Vector3 _rawInputMovement;
        private float _currentSpeed;

        void Start()
        {
            _rb = GetComponent<Rigidbody>();
            anim = GetComponent<Animator>();

            input = PlayerInputHandler.Instance;
            cameraTransform = Camera.main.transform;
        }

        void FixedUpdate()
        {
            HandleMovement();
        }
        

        private void HandleMovement()
        {
            Vector2 inputMovement = input.MoveInput;
            _rawInputMovement = GetCameraRelativeMovement(inputMovement);
            float targetSpeed = Mathf.Lerp(movementSpeed, runSpeed, input.MoveSpeedMultiplier);
        
            if (input.IsAiming)
            {
                _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed/1.5f, Time.deltaTime * 10f);

            }
            else
            {
                _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.deltaTime * 10f);
            }

            anim.SetFloat("Speed", _rawInputMovement.magnitude < 0.1f ? 0f : Mathf.Lerp(anim.GetFloat("Speed"), _rawInputMovement.magnitude * (targetSpeed / runSpeed), Time.deltaTime * 10f));
        
            Vector3 moveDirection = _rawInputMovement * (_currentSpeed * Time.deltaTime);
            _rb.MovePosition(_rb.position + moveDirection);

            if (_rawInputMovement.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_rawInputMovement);
                if (!input.IsAiming)
                {
                
                    _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRotation, turnSpeed * Time.deltaTime);

                }
            }
        }
        
        private Vector3 GetCameraRelativeMovement(Vector2 cameraInput)
        {
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            return (cameraForward.normalized * cameraInput.y + cameraRight.normalized * cameraInput.x).normalized;
        }
    }
}
