using System.Collections;
using Player;
using UnityEngine;

namespace AbilitySystem
{
    public class CloneMovement : MonoBehaviour
    {
        public Animator anim;
        public Transform cameraTransform;

        [SerializeField] private MonoBehaviour inputSourceRef;
        private IPlayerInputSource input;

        private Rigidbody _rb;

        [Header("Movement Settings")] 
        [SerializeField] private float movementSpeed = 2f;
        [SerializeField] private float runSpeed = 4f;
        [SerializeField] private float turnSpeed = 10f;

        [Header("Jump Settings")] 
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private int maxJumpCount = 2;
        private int currentJumpCount = 0;

        [Header("Dash Settings")] 
        [SerializeField] private float dashSpeed = 12f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 0.6f;

        private bool canDash = true;
        private bool isDashing = false;

        private Vector3 _rawInputMovement;
        private float _currentSpeed;

        void Start()
        {
            _rb = GetComponent<Rigidbody>();
            anim = GetComponent<Animator>();

            input = inputSourceRef as IPlayerInputSource;
            if (input == null)
                Debug.LogError("Input source 不符合 IPlayerInputSource");
        }

        void FixedUpdate()
        {
            ReadInputState();
            HandleMovement();
        }

        private void ReadInputState()
        {
            if (input.JumpPressed && currentJumpCount < maxJumpCount)
            {
                HandleJump();
            }

            if (input.DashPressed && canDash && !isDashing)
            {
                StartCoroutine(HandleDash());
            }
        }

        private void HandleMovement()
        {
            if (isDashing) return;
        
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

        private void HandleJump()
        {
            currentJumpCount++;

            if (currentJumpCount == 1)
            {
                anim.SetBool("Jump", true);
                anim.SetBool("IsDoubleJump", false);
            }
            else if (currentJumpCount == 2)
            {
                anim.SetBool("IsDoubleJump", true);
                anim.SetBool("Jump", false);
            }

            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, jumpForce, _rb.linearVelocity.z);
        }

        private IEnumerator HandleDash()
        {
            isDashing = true;
            canDash = false;
            anim.SetBool("Dash", true);

            Vector3 dashDir = (_rawInputMovement.magnitude > 0.1f) ? _rawInputMovement.normalized : transform.forward;

            float timer = 0f;
            while (timer < dashDuration)
            {
                _rb.linearVelocity = dashDir * dashSpeed;
                timer += Time.deltaTime;
                yield return null;
            }

            _rb.linearVelocity = Vector3.zero;
            anim.SetBool("Dash", false);
            isDashing = false;

            yield return new WaitForSeconds(dashCooldown);
            canDash = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.contacts[0].normal.y > 0.5f)
            {
                currentJumpCount = 0;
                anim.SetBool("Jump", false);
                anim.SetBool("IsDoubleJump", false);
            }
        }
    }
}
