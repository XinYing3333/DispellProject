using System;
using System.Collections;
using Cinemachine;
using Player;
using UnityEngine;
using UnityEngine.VFX;

public class PlayerMovement : MonoBehaviour
{
    public Animator anim;
    public Transform cameraTransform;
    
    private PlayerInputHandler input;
    
    private Rigidbody _rb;

    [Header("Camera Settings")] 
    [SerializeField] private CinemachineFreeLook mainCam;
    [SerializeField] private CinemachineFreeLook elephantCam;
    
    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float turnSpeed = 20f;
    [SerializeField] private VisualEffect stepVFX;
    [SerializeField] private ParticleSystem jumpVFX;
    [SerializeField] private ParticleSystem groundedVFX;
    
    [Header("Jump Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private int maxJumpCount = 2;
    [SerializeField] private PhysicsMaterial defaultMaterial;
    [SerializeField] private PhysicsMaterial noFrictionMaterial;
    private Collider _playerCollider;
    private int currentJumpCount = 0;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.6f;
    
    [Header("Grab Settings")]
    [SerializeField] private LayerMask ledgeLayer;
    [SerializeField] private float grabOffset = 1.5f; // 微調吸到邊的偏移
    [SerializeField] private float grabDetectionHeight = 1.2f; // 玩家高於這個點才能抓
    [SerializeField] private float ledgeCheckDistance = 0.5f; // 檢測前方距離
    
    [Header("Detect Lock")]
    [SerializeField] public bool isWalkOnly = false;     // 教學期間=只能走
    [SerializeField] public bool lockJumpInWalkOnly = false;
    [SerializeField] public bool lockDashInWalkOnly  = false;
    
    private bool isGrabbing;
    private bool isFinishClimb;
    private bool isPushing;
    private Collider currentCollider;

    
    private bool canDash = true;
    private bool isDashing = false;
    
    private bool isFootstepPlaying = false;
    private bool isOnGround = false;
    
    private Vector3 _rawInputMovement;
    private float _currentSpeed;
    
    private bool hasPlayedGroundedVFX = false;


    public void ApplyElephantStats()
    {
        movementSpeed = 3.5f;
        runSpeed = 7f;
        jumpForce = 12f;
        dashSpeed = 15f;
        anim.SetBool("IsRidingElephant", true);
        //EventBus<ChangeCameraEvent>.Publish(new ChangeCameraEvent(elephantCam));
    }

    public void RestoreDefaultStats()
    {
        movementSpeed = 2f;
        runSpeed = 4.5f;
        jumpForce = 10f;
        dashSpeed = 12f;
        anim.SetBool("IsRidingElephant", false);
        //EventBus<ChangeCameraEvent>.Publish(new ChangeCameraEvent(mainCam));

    }
    
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        _playerCollider = GetComponent<Collider>();

        input = PlayerInputHandler.Instance;
        if (input == null)
            Debug.LogError("沒有獲取input");

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        stepVFX.Stop();
        
    }

    void Update()
    {
        SetAnimatorLayerWeight("Inhale", input.IsCollecting ? 1f : 0f);//--------------------------------------------
        SetAnimatorLayerWeight("Shoot", input.ShootPressed ? 1f : 0f);//--------------------------------------------
        //SetAnimatorLayerWeight("Pray", input.ShootPressed ? 1f : 0f);//--------------------------------------------

        //SwitchJumpFriction();
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
                OnJump();
            }
        }
        else
        {
            CheckForLedgeGrab();
        }
        
        StepClimbCheck();
        CheckWallFriction();
        UpdateFootstepAudio();

        bool isGrounded = IsGrounded();

        if (isGrounded && !hasPlayedGroundedVFX)
        {
            groundedVFX.Play();
            hasPlayedGroundedVFX = true;
        }
        else if (!isGrounded)
        {
            // 當離開地面後，重置狀態，準備下次著陸時再次觸發
            hasPlayedGroundedVFX = false;
        }
    }

    /*void SwitchJumpFriction()
    {
        if (IsGrounded())
        {
            _playerCollider.material = defaultMaterial;
        }
        else
        {
            _playerCollider.material = noFrictionMaterial;
        }
    }*/
    
    private void CheckWallFriction()
    {
        bool touchingWall = false;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3[] dirs = { transform.forward, -transform.forward, transform.right, -transform.right };

        foreach (var dir in dirs)
        {
            if (Physics.Raycast(origin, dir, out RaycastHit hit, 0.6f, groundLayer))
            {
                float wallAngle = Vector3.Dot(hit.normal, Vector3.up);
                if (Mathf.Abs(wallAngle) < 0.2f)
                {
                    touchingWall = true;
                    break;
                }
            }
        }

        _playerCollider.material = touchingWall ? noFrictionMaterial : defaultMaterial;
    }
    
    bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, 0.2f, groundLayer);
    }

    
    
    private void SetAnimatorLayerWeight(string layerName, float weight)
    {
        int layerIndex = anim.GetLayerIndex(layerName);
        if (layerIndex != -1)
        {
            anim.SetLayerWeight(layerIndex, weight);
        }
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
            
            _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRotation, turnSpeed * Time.deltaTime);
            
            //移動時快速 Recenter
            mainCam.m_RecenterToTargetHeading.m_WaitTime = 0.5f;
            mainCam.m_RecenterToTargetHeading.m_RecenteringTime = 0.8f;
        }
        else
        {
            //未移動時慢速 Recenter
            mainCam.m_RecenterToTargetHeading.m_WaitTime = 5f;
            mainCam.m_RecenterToTargetHeading.m_RecenteringTime = 2.5f;
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
                stepVFX.Stop();
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
            stepVFX.Play();
            AudioManager.Instance.PlaySFXLoop(moveState);
            isFootstepPlaying = true;
            currentMoveState = moveState;
        }
    }
    
    private string DetectSurfaceType()
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
    }

    [SerializeField] private float maxStepHeight = 0.3f;
    [SerializeField] private float stepCheckDistance = 0.4f;

    private void StepClimbCheck()
    {
        Vector3 origin = transform.position + Vector3.up * 0.05f;
        Vector3 direction = _rawInputMovement.normalized;

        if (direction == Vector3.zero) return;

        // 檢查腳邊碰撞
        if (Physics.Raycast(origin, direction, out RaycastHit lowerHit, stepCheckDistance, groundLayer))
        {
            // 從上方高度發出射線檢查是否能通過
            Vector3 upperOrigin = transform.position + Vector3.up * maxStepHeight;
            if (!Physics.Raycast(upperOrigin, direction, stepCheckDistance, groundLayer))
            {
                // 沒有上方障礙，可以往上爬
                _rb.position += Vector3.up * 0.1f;
            }
        }
    }

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
            jumpVFX.Play();
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
        Vector3 forwardStart = transform.position + Vector3.up * 1.5f;
        Vector3 forwardDir = transform.forward;

        Debug.DrawRay(forwardStart, forwardDir * 0.6f, Color.blue);

        if (Physics.Raycast(forwardStart, forwardDir, out RaycastHit forwardHit, 0.6f, ledgeLayer))
        {
            Vector3 downStart = forwardHit.point + Vector3.up * 0.3f;

            Debug.DrawRay(downStart, Vector3.down * 1.0f, Color.yellow);

            if (Physics.Raycast(downStart, Vector3.down, out RaycastHit downHit, 1.0f, ledgeLayer))
            {
                float angle = Vector3.Angle(downHit.normal, Vector3.up);

                if (angle < 45f)
                {
                    StartLedgeGrab(downHit.point);
                }
            }
        }
    }


    private void StartLedgeGrab(Vector3 ledgePoint)
    {
        if (isGrabbing) return;

        isGrabbing = true;
        _rb.linearVelocity = Vector3.zero;
        _rb.useGravity = false;
        transform.position = ledgePoint + Vector3.down * grabOffset;
        anim.SetBool("IsLedgeGrabbing", true);
        input.ResetJump();
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
        //input.ResetJump();
        yield return new WaitForSeconds(0.1f);
        if (isGrabbing)
        {
            /*transform.position = new Vector3(transform.position.x, currentCollider.bounds.center.y + currentCollider.bounds.size.y 
                * 0.5f + 0.03f, transform.position.z);
            transform.position += transform.forward;*/
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
        if(other.gameObject.CompareTag("Object"))
        {
            if (_rawInputMovement.magnitude > 0.1f)
            {
               anim.SetBool("isPush", true);
            }
        }
    }
    
    private void OnCollisionStay(Collision other)
    {
        if(other.gameObject.CompareTag("Object"))
        {
            switch (_rawInputMovement.magnitude)
            {
                case < 0.1f:
                    anim.SetBool("isPush", false);
                    isPushing = false;

                    break;
                case > 0.1f:
                    
                    anim.SetBool("isPush", true);
                    isPushing = true;

                    break;
            }
        }
    }
    
    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            isOnGround = false;
        }
        if(other.gameObject.CompareTag("Object"))
        {
            anim.SetBool("isPush", false);
            isPushing = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("CenterPoint"))
        {
            anim.SetTrigger("isPraying");
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
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        
        Gizmos.color = Color.red;
        Vector3 rayStart = transform.position + Vector3.up * grabDetectionHeight;
        Vector3 rayDir = transform.forward;

        Gizmos.DrawLine(rayStart, rayStart + rayDir * ledgeCheckDistance);
        Gizmos.DrawWireSphere(rayStart + rayDir * ledgeCheckDistance + Vector3.up * 0.5f, 0.2f);
    }
#endif
}
