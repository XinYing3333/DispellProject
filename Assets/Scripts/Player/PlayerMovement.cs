using System.Collections;
using Player;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Animator anim;
    public Transform cameraTransform;
    
    private PlayerInputHandler input;
    
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
    
    [Header("Grab Settings")]
    [SerializeField] private LayerMask ledgeLayer;
    [SerializeField] private float grabOffset = 0.5f; // 微調吸到邊的偏移
    [SerializeField] private float grabDetectionHeight = 1.2f; // 玩家高於這個點才能抓
    [SerializeField] private float ledgeCheckDistance = 0.5f; // 檢測前方距離
    private bool isGrabbing;
    private bool isFinishClimb;
    private Collider currentCollider;

    
    private bool canDash = true;
    private bool isDashing = false;
    
    private bool isFootstepPlaying = false;
    private bool isOnGround = false;
    
    private Vector3 _rawInputMovement;
    private float _currentSpeed;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        input = PlayerInputHandler.Instance;
        if (input == null)
            Debug.LogError("沒有獲取input");

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }


    void FixedUpdate()
    {
        if (!isGrabbing)
        {
            OnMovement();
            if (input.JumpPressed)
            {
                OnJump();
                input.ResetJump();
            }
            if (input.DashPressed)
            {
                StartCoroutine(DashCoroutine());
                input.ResetDash();
            }
        }
        
        if (isGrabbing)
        {
            HandleLedgeMovement();

            if (input.JumpPressed)
            {
                StartCoroutine(ReleaseLedge());
            }
        }
        else
        {
            CheckForLedgeGrab();
        }
        
        UpdateFootstepAudio();
    }

    private void OnMovement()
    {
        if (isDashing) return;
        
        Vector2 inputMovement = input.MoveInput;
        _rawInputMovement = GetCameraRelativeMovement(inputMovement);
        float targetSpeed = Mathf.Lerp(movementSpeed, runSpeed, input.MoveSpeedMultiplier);
        
        if (input.IsCollecting)
        {
            _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed/2f, Time.deltaTime * 10f);

        }
        else
        {
            _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.deltaTime * 10f);
        }

        anim.SetFloat("Speed", _rawInputMovement.magnitude < 0.1f ? 
            0f : Mathf.Lerp(anim.GetFloat("Speed"), _rawInputMovement.magnitude * (targetSpeed / runSpeed), Time.deltaTime * 10f));
        
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
    
    private string currentSurface = "Default"; 
    private SFXType currentMoveState; 

    private void UpdateFootstepAudio()
    {

        if (!isOnGround || isDashing || _rawInputMovement.magnitude <= 0.1f)
        {
            if (isFootstepPlaying)
            {
                AudioManager.Instance.StopSFXLoop();
                isFootstepPlaying = false;
                //currentMoveState = "";
            }
            return;
        }

        SFXType moveState = input.MoveSpeedMultiplier > 0.5f ? SFXType.Run : SFXType.Walk;
        //string surface = DetectSurfaceType();
        //string sfxName = $"{surface}_{moveState}";

        if (!isFootstepPlaying || currentMoveState != moveState)
        {
            AudioManager.Instance.PlaySFXLoop(moveState);
            isFootstepPlaying = true;
            currentMoveState = moveState;
        }
    }
    
    /*private string DetectSurfaceType()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 1.5f))
        {
            string tag = hit.collider.tag;
            switch (tag)
            {
                case "Grass": return "Grass";
                case "Stone": return "Stone";
                case "Wood": return "Wood";
                default: return "Default";
            }
        }
        return "Default";
    }*/



    private void OnJump()
    {
        if (currentJumpCount >= maxJumpCount) return;
        
        currentJumpCount++;
        
        if (currentJumpCount == 1)
        {
            anim.SetBool("Jump", true);
            anim.SetBool("IsDoubleJump", false); // 確保不是二段跳
            AudioManager.Instance.PlaySFX(SFXType.Jump);
        }
        else if (currentJumpCount == 2)
        {
            anim.SetBool("IsDoubleJump", true);
            anim.SetBool("Jump", false); // 防止影響主跳躍動畫
            AudioManager.Instance.PlaySFX(SFXType.Jump);
        }
        
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, jumpForce, _rb.linearVelocity.z);
    }
    
    private IEnumerator DashCoroutine()
    {
        if (!canDash || isDashing) yield break;
        
        isDashing = true;
        canDash = false;
        anim.SetBool("Dash", true);
        AudioManager.Instance.PlaySFX(SFXType.Dash);

        Vector3 dashDirection = (_rawInputMovement.magnitude > 0.1f) ? _rawInputMovement.normalized : transform.forward;
        float startTime = Time.time;

        while (Time.time < startTime + dashDuration)
        {
            _rb.linearVelocity = dashDirection * dashSpeed;
            yield return null; 
        }

        _rb.linearVelocity = Vector3.zero;
        anim.SetBool("Dash", false);
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void CheckForLedgeGrab()
    {
        Vector3 rayStart = transform.position + Vector3.up * grabDetectionHeight;
        Ray ray = new Ray(rayStart, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, ledgeCheckDistance, ledgeLayer))
        {
            StartLedgeGrab(hit.point);
        }
    }

    private void StartLedgeGrab(Vector3 ledgePoint)
    {
        isGrabbing = true;
        _rb.linearVelocity = Vector3.zero;
        _rb.useGravity = false;
        transform.position = ledgePoint + Vector3.down * grabOffset;
        anim.SetBool("IsLedgeGrabbing", true);
    }

    private void HandleLedgeMovement()
    {
        /*Vector2 moveInput = input.MoveInput;
        Vector3 move = transform.right * moveInput.x * climbSpeed * Time.deltaTime;
        transform.position += move;*/
    }

    private IEnumerator ReleaseLedge()
    {
        anim.SetBool("IsLedgeGrabbing", false);
        input.ResetJump();
        yield return new WaitForSeconds(3.6f / 2);
        if (isGrabbing)
        {
            transform.position = new Vector3(transform.position.x, currentCollider.bounds.center.y + currentCollider.bounds.size.y 
                * 0.5f + 0.03f, transform.position.z);
            transform.position += transform.forward;
            _rb.useGravity = true;
        }
        isGrabbing = false;
    }


    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            isOnGround = true;

            anim.SetBool("Jump", false);
            anim.SetBool("IsDoubleJump", false);

            currentJumpCount = 0;
        }
        if (other.gameObject.layer == LayerMask.NameToLayer("Ledge"))
        {
            currentCollider = other.collider;
        }
    }
    
    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            isOnGround = false;
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
