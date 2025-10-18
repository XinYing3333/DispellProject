using System;
using System.Collections;
using Cinemachine;
using Player;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Refs")]
    public Animator anim;
    public Transform cameraTransform;

    private PlayerInputHandler input;
    private Rigidbody _rb;
    private Collider _col;

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

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.6f;

    [Header("Grab Settings")]
    [SerializeField] private LayerMask ledgeLayer;
    [SerializeField] private float grabOffset = 1.5f;
    [SerializeField] private float grabDetectionHeight = 1.2f;
    [SerializeField] private float ledgeCheckDistance = 0.5f;

    [Header("Detect Lock")]
    [SerializeField] public bool isWalkOnly = false;
    [SerializeField] public bool lockJumpInWalkOnly = false;
    [SerializeField] public bool lockDashInWalkOnly = false;

    // 在 PlayerMovement 裡加入：
    [Header("Safe Ground Tracker")]
    [SerializeField] private float stableTimeThreshold = 0.2f;  // 站穩多久才算安全
    [SerializeField] private float maxHorizontalSpeed = 7f;     // 移動太快不記錄
    [SerializeField] private LayerMask safeGroundMask;          // 可踩的地面層（可用原 groundLayer）

    private float stableTimer = 0f;
    private bool hasSafeGround = false;
    private Vector3 lastSafePos;
    private Quaternion lastSafeRot;
    
    // 狀態
    private bool isGrabbing;
    private bool isFinishClimb;
    private bool isPushing;
    private Collider currentCollider;

    private bool canDash = true;
    private bool isDashing = false;

    private bool isFootstepPlaying = false;
    private bool isOnGround = false;     // 由 IsGrounded() 得到
    private bool wasOnGround = false;    // 邊界觸發用

    private Vector3 _rawInputMovement;   // 相機相對方向
    private float _currentSpeed;

    private bool hasPlayedGroundedVFX = false;

    // 腳步聲/材質
    private string currentSurface = "Default";
    private SFXType currentMoveState;

    // 跨幀記錄：相機 recentre 切換
    private bool _wasMoving = false;

    // 跨幀記錄：Dash 方向（由協程決定，FixedUpdate 消費）
    private bool _dashActive = false;
    private Vector3 _dashDir = Vector3.zero;

    // 跨幀記錄：動畫速度參數（Update 計算，FixedUpdate 不碰 Animator）
    private float _animSpeedParam = 0f;

    // Step 爬階
    [SerializeField] private float maxStepHeight = 0.3f;
    [SerializeField] private float stepCheckDistance = 0.4f;

    // ====== 生命週期 ======
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        anim = GetComponent<Animator>();

        // 建議的物理設定，讓視覺更順
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.freezeRotation = true; // 由我們用 MoveRotation 控制朝向
    }

    private void Start()
    {
        input = PlayerInputHandler.Instance;
        if (input == null) Debug.LogError("沒有獲取 PlayerInputHandler");

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (stepVFX != null) stepVFX.Stop();
    }

    // ====== Update：輸入、動畫、相機切換 ======
    private void Update()
    {
        if (input == null) return;

        // 讀輸入 -> 算相機相對方向（只算目標，不做物理）
        Vector2 moveInput = input.MoveInput;
        _rawInputMovement = GetCameraRelativeMovement(moveInput);

        // 目標速度
        float targetSpeed = Mathf.Lerp(movementSpeed, runSpeed, input.MoveSpeedMultiplier);
        if (input.IsCollecting) targetSpeed *= 0.5f;
        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, 10f * Time.deltaTime);

        // Animator 層權重
        SetAnimatorLayerWeight("Inhale", input.IsCollecting ? 1f : 0f);
        SetAnimatorLayerWeight("Shoot", input.ShootPressed ? 1f : 0f);

        // 動畫速度參數（僅 Update 更動）
        _animSpeedParam = (_rawInputMovement.magnitude < 0.1f)
            ? 0f
            : Mathf.Lerp(anim.GetFloat("Speed"),
                         _rawInputMovement.magnitude * (targetSpeed / runSpeed),
                         10f * Time.deltaTime);
        anim.SetFloat("Speed", _animSpeedParam);

        // 觸發行為（只下指令，不直接動 Rigidbody）
        if (!PlayerInputHandler.Instance.cannotMove && !isGrabbing)
        {
            if (!isWalkOnly || (isWalkOnly && !lockJumpInWalkOnly))
            {
                if (input.JumpPressed)
                {
                    TryJump();
                    input.ResetJump();
                }
            }

            if (!isWalkOnly || (isWalkOnly && !lockDashInWalkOnly))
            {
                if (input.DashPressed)
                {
                    StartCoroutine(DashCoroutine()); // 協程內用 WaitForFixedUpdate
                    input.ResetDash();
                }
            }
        }

        // 抓邊流程的輸入（放這裡僅觸發，實際位移於 FixedUpdate）
        if (isGrabbing)
        {
            if (input.JumpPressed)
            {
                StartCoroutine(ReleaseLedge());
                TryJump(); // 放手後立刻跳
            }
        }

        // 相機 recentring 只在「移動/停止」切換時調一次
        bool isMoving = _rawInputMovement.sqrMagnitude > 0.01f;
        if (isMoving != _wasMoving)
        {
            if (isMoving)
            {
                mainCam.m_RecenterToTargetHeading.m_enabled = true;
                mainCam.m_RecenterToTargetHeading.m_WaitTime = 0.5f;
                mainCam.m_RecenterToTargetHeading.m_RecenteringTime = 0.8f;
            }
            else
            {
                mainCam.m_RecenterToTargetHeading.m_enabled = false;
            }
            _wasMoving = isMoving;
        }
        TrackSafeGround();
    }

    // ====== FixedUpdate：物理唯一來源 ======
    private void FixedUpdate()
    {
        if (PlayerInputHandler.Instance.cannotMove)
        {
            // 清理運動狀態
            _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
            return;
        }

        // 地面檢測（物理步統一）
        wasOnGround = isOnGround;
        isOnGround = IsGrounded();

        // 落地/離地事件（VFX/音）
        if (isOnGround && !wasOnGround)
        {
            if (groundedVFX != null) groundedVFX.Play();
            ResetJump();
            hasPlayedGroundedVFX = true;
        }
        else if (!isOnGround && wasOnGround)
        {
            hasPlayedGroundedVFX = false;
        }

        // 抓邊檢測（未抓時才檢查）
        if (!isGrabbing) CheckForLedgeGrab();

        // 牆面摩擦（若要做可在這裡切材質）
        CheckWallFriction();

        // 爬階（物理步）
        StepClimbCheck();

        // Dash 期間速度由 dash 狀態主宰
        if (_dashActive)
        {
            _rb.linearVelocity = new Vector3(_dashDir.x * dashSpeed, _rb.linearVelocity.y, _dashDir.z * dashSpeed);
        }
        else if (!isGrabbing)
        {
            // 一般移動（MovePosition）
            if (!isDashing)
            {
                Vector3 move = _rawInputMovement * (_currentSpeed * Time.fixedDeltaTime);
                _rb.MovePosition(_rb.position + move);

                if (_rawInputMovement.sqrMagnitude > 0.01f)
                {
                    var targetRot = Quaternion.LookRotation(_rawInputMovement);
                    var newRot = Quaternion.Slerp(_rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
                    _rb.MoveRotation(newRot);
                }
            }
        }

        // 抓邊期間：鎖住剛體於 ledge 點附近（若有側移可在這裡做）
        if (isGrabbing)
        {
            // 可在這裡做沿邊水平移動的 rb.MovePosition
        }

        // 腳步聲 state 機（根據 isOnGround + 速度狀態）
        UpdateFootstepAudio();
    }

    // ====== 介面：坐騎切換 ======
    public void ApplyElephantStats()
    {
        movementSpeed = 3.5f;
        runSpeed = 7f;
        jumpForce = 12f;
        dashSpeed = 15f;
        anim.SetBool("IsRidingElephant", true);
    }

    public void RestoreDefaultStats()
    {
        movementSpeed = 2f;
        runSpeed = 4.5f;
        jumpForce = 10f;
        dashSpeed = 12f;
        anim.SetBool("IsRidingElephant", false);
    }

    // ====== 動畫層權重 ======
    private void SetAnimatorLayerWeight(string layerName, float weight)
    {
        int idx = anim.GetLayerIndex(layerName);
        if (idx != -1) anim.SetLayerWeight(idx, weight);
    }

    // ====== 行為：跳躍（僅下指令，實際由物理接手） ======
    private int currentJumpCount = 0;

    private void TryJump()
    {
        if (isWalkOnly && lockJumpInWalkOnly) return;
        if (currentJumpCount >= maxJumpCount) return;

        currentJumpCount++;

        if (currentJumpCount == 1)
        {
            anim.SetBool("Jump", true);
            anim.SetBool("IsDoubleJump", false);
            AudioManager.Instance.PlaySFX(SFXType.Jump);
        }
        else if (currentJumpCount == 2)
        {
            anim.SetBool("IsDoubleJump", true);
            anim.SetBool("Jump", false);
            AudioManager.Instance.PlaySFX(SFXType.Jump);
            if (jumpVFX != null) jumpVFX.Play();
        }

        // 在物理步生效：直接改剛體的垂直速度
        var v = _rb.linearVelocity;
        v.y = jumpForce;
        _rb.linearVelocity = v;
    }

    void ResetJump()
    {
        currentJumpCount = 0;
        anim.SetBool("Jump", false);
        anim.SetBool("IsDoubleJump", false);
    }

    // ====== 行為：Dash（用 WaitForFixedUpdate 與狀態） ======
    private IEnumerator DashCoroutine()
    {
        if (isWalkOnly && lockDashInWalkOnly) yield break;
        if (!canDash || isDashing) yield break;

        isDashing = true;
        canDash = false;
        anim.SetBool("Dash", true);
        AudioManager.Instance.PlaySFX(SFXType.Dash);

        _dashDir = (_rawInputMovement.sqrMagnitude > 0.01f) ? _rawInputMovement.normalized : transform.forward;
        _dashActive = true;

        float t = 0f;
        while (t < dashDuration)
        {
            yield return new WaitForFixedUpdate();      // ★ 物理步同步
            t += Time.fixedDeltaTime;
        }

        _dashActive = false;
        anim.SetBool("Dash", false);
        isDashing = false;

        // 不硬清零，交回給一般移動；若需要可做短阻尼
        // _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // ====== 地面/牆面/樓梯 ======
    private bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, 0.2f, groundLayer, QueryTriggerInteraction.Ignore);
    }

    private void CheckWallFriction()
    {
        // 若要做黏牆材質切換，可在此啟用
        // bool touchingWall = false;
        // ...
        // _col.material = touchingWall ? noFrictionMaterial : defaultMaterial;
    }

    private void StepClimbCheck()
    {
        if (_rawInputMovement.sqrMagnitude < 0.0001f) return;

        Vector3 direction = _rawInputMovement.normalized;
        Vector3 lowerOrigin = transform.position + Vector3.up * 0.05f;
        if (Physics.Raycast(lowerOrigin, direction, out RaycastHit lowerHit, stepCheckDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            Vector3 upperOrigin = transform.position + Vector3.up * maxStepHeight;
            if (!Physics.Raycast(upperOrigin, direction, stepCheckDistance, groundLayer, QueryTriggerInteraction.Ignore))
            {
                // 平滑拉升一點（用 MovePosition 而非 transform）
                _rb.MovePosition(_rb.position + Vector3.up * 0.1f);
            }
        }
    }

    // ====== 腳步音與材質 ======
    private void UpdateFootstepAudio()
    {
        bool movingHoriz = _rawInputMovement.magnitude > 0.1f;
        bool shouldPlay = isOnGround && !isDashing && movingHoriz;

        if (!shouldPlay && isFootstepPlaying)
        {
            AudioManager.Instance.StopSFXLoop();
            if (stepVFX != null) stepVFX.Stop();
            isFootstepPlaying = false;
            return;
        }

        if (shouldPlay)
        {
            SFXType moveState = (input != null && input.MoveSpeedMultiplier > 0.5f) ? SFXType.Run : SFXType.Walk;
            if (!isFootstepPlaying || currentMoveState != moveState)
            {
                if (stepVFX != null) stepVFX.Play();
                AudioManager.Instance.PlaySFXLoop(moveState);
                isFootstepPlaying = true;
                currentMoveState = moveState;
            }
        }
    }

    private string DetectSurfaceType()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 1.5f, ~0, QueryTriggerInteraction.Ignore))
        {
            string tag = hit.collider.tag;
            switch (tag)
            {
                case "Grass": return "Grass";
                case "Stone": return "Stone";
                case "Wood":  return "Wood";
                default:      return "Default";
            }
        }
        return "Default";
    }

    // ====== 抓邊 ======
    private void CheckForLedgeGrab()
    {
        Vector3 forwardStart = transform.position + Vector3.up * Mathf.Max(1.5f, grabDetectionHeight);
        Vector3 forwardDir = transform.forward;

        Debug.DrawRay(forwardStart, forwardDir * 0.6f, Color.blue);

        if (Physics.Raycast(forwardStart, forwardDir, out RaycastHit forwardHit, 0.6f, ledgeLayer, QueryTriggerInteraction.Ignore))
        {
            Vector3 downStart = forwardHit.point + Vector3.up * 0.3f;
            Debug.DrawRay(downStart, Vector3.down * 1.0f, Color.yellow);

            if (Physics.Raycast(downStart, Vector3.down, out RaycastHit downHit, 1.0f, ledgeLayer, QueryTriggerInteraction.Ignore))
            {
                float angle = Vector3.Angle(downHit.normal, Vector3.up);
                if (angle < 45f) StartLedgeGrab(downHit.point);
            }
        }
    }

    private void StartLedgeGrab(Vector3 ledgePoint)
    {
        if (isGrabbing) return;

        isGrabbing = true;

        // 切到「抓邊」的剛體型態（避免與物理搶位置造成 jitter）
        _rb.linearVelocity = Vector3.zero;
        _rb.useGravity = false;
        _rb.isKinematic = true;

        // 用剛體座標，不用 transform
        _rb.position = ledgePoint + Vector3.down * grabOffset;

        anim.SetBool("IsLedgeGrabbing", true);
        input.ResetJump();
    }

    private IEnumerator ReleaseLedge()
    {
        anim.SetBool("IsLedgeGrabbing", false);
        yield return new WaitForSeconds(0.1f);

        if (isGrabbing)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
        }
        isGrabbing = false;
    }
    
    private void TrackSafeGround()
    {
        // 確認是否踩地
        if (!IsGrounded()) { stableTimer = 0f; return; }

        // 判斷水平速度是否太快（例如跳或滑落邊緣）
        Vector3 horizontalVel = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        if (horizontalVel.magnitude > maxHorizontalSpeed)
        {
            stableTimer = 0f;
            return;
        }

        // 穩定計時
        stableTimer += Time.deltaTime;
        if (stableTimer >= stableTimeThreshold)
        {
            lastSafePos = transform.position;
            lastSafeRot = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            hasSafeGround = true;
        }
    }

    public bool TryGetLastSafeGround(out Vector3 pos, out Quaternion rot)
    {
        pos = default;
        rot = default;
        if (!hasSafeGround) return false;
        pos = lastSafePos;
        rot = lastSafeRot;
        return true;
    }

    // ====== 碰撞處理（只做狀態切換；地面狀態以 Raycast 為準） ======
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ledge"))
            currentCollider = other.collider;

        if (other.gameObject.CompareTag("Object"))
        {
            if (_rawInputMovement.magnitude > 0.1f)
                anim.SetBool("isPush", true);
        }
    }

    private void OnCollisionStay(Collision other)
    {
        if (!other.gameObject.CompareTag("Object")) return;

        if (_rawInputMovement.magnitude < 0.1f)
        {
            anim.SetBool("isPush", false);
            isPushing = false;
        }
        else
        {
            anim.SetBool("isPush", true);
            isPushing = true;
        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.CompareTag("Object"))
        {
            anim.SetBool("isPush", false);
            isPushing = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("CenterPoint"))
            anim.SetTrigger("isPraying");
    }

    // ====== 工具 ======
    private Vector3 GetCameraRelativeMovement(Vector2 cameraInput)
    {
        Vector3 f = cameraTransform.forward; f.y = 0f;
        Vector3 r = cameraTransform.right;   r.y = 0f;
        return (f.normalized * cameraInput.y + r.normalized * cameraInput.x).normalized;
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
