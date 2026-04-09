using System;
using System.Collections;
using Cinemachine;
using EventBus.Events.Health;
using Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.VFX;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PlayerMovement : MonoBehaviour
{
    // ==========================================
    // 參照與元件
    // ==========================================
    [Header("Refs")] public Animator anim;
    public Transform cameraTransform;

    private PlayerInputHandler input;
    private Rigidbody _rb;
    private Collider _col;

    // ==========================================
    // 參數設定區 (Inspector)
    // ==========================================
    [Header("Camera Settings")] [SerializeField]
    private CinemachineFreeLook mainCam;

    [SerializeField] private CinemachineFreeLook elephantCam;

    [Header("Movement Settings")] [SerializeField]
    private float movementSpeed = 2f;

    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float turnSpeed = 20f;
    [SerializeField] private float aimTurnSpeed = 50f; // ★ 新增：瞄準時的旋轉速度（建議設為原來的 2-3 倍）
    [SerializeField] private float maxStepHeight = 0.3f;
    [SerializeField] private float stepCheckDistance = 0.4f;

    [Header("VFX Settings")]
    [SerializeField] private ParticleSystem stepVFX;
    [SerializeField] private ParticleSystem hurtVFX;

    [SerializeField] private ParticleSystem firstJumpVFX;
    [SerializeField] private ParticleSystem doubleJumpVFX;
    [SerializeField] private ParticleSystem groundedVFX;

    [Header("Jump Settings")] [SerializeField]
    private LayerMask groundLayer;

    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private int maxJumpCount = 2;
    [SerializeField] private PhysicsMaterial defaultMaterial;
    [SerializeField] private PhysicsMaterial noFrictionMaterial;

    [Header("Ground Control (Scheme A)")] [SerializeField, Tooltip("可站立地面最大角度")]
    private float maxGroundAngle = 45f;

    [SerializeField, Tooltip("地面加速（Acceleration 模式）")]
    private float groundAccel = 55f;

    [SerializeField, Tooltip("地面煞車（無輸入時減速）")]
    private float groundBrake = 70f;

    [SerializeField, Tooltip("空中水平控制力")] private float airAccel = 18f;

    [SerializeField, Tooltip("貼地力（讓坡面更穩，不影響跳躍）")]
    private float stickToGroundForce = 25f;

    [Header("Wall Clamp (Scheme A)")] [SerializeField, Tooltip("法線與Up的dot越小越像牆")]
    private float wallNormalUpDotMax = 0.22f;

    [SerializeField, Tooltip("避免把陡斜面當牆：牆法線與Up的角度需大於此值")]
    private float wallMinAngleFromUp = 80f;

    [SerializeField, Tooltip("貼牆力度（越大越黏，沿牆更難脫離）")]
    private float wallStickStrength = 10f;

    [SerializeField, Tooltip("貼牆時最大下落速度（做出貼牆滑）")]
    private float wallSlideDownSpeed = 3.5f;

    [SerializeField, Tooltip("水平速度太大時不吸（避免Dash擦牆被吸死）")]
    private float wallReleaseSpeed = 5f;

    [SerializeField, Tooltip("上升速度低於此值才允許黏（0=上升也黏）")]
    private float wallMinUpVelToStick = 0f;

    [SerializeField, Tooltip("玩家持續往牆推時，強制下滑速度（>0）")]
    private float wallForcedSlideSpeed = 6f;

    [SerializeField, Tooltip("判定為「在推牆」：moveDir 與 -wallNormal 的點積門檻")]
    private float wallPressDotThreshold = 0.25f;

    [Header("Dash Settings")] [SerializeField]
    private float dashSpeed = 12f;

    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.6f;

    [Header("Detect Lock")] [SerializeField]
    public bool isWalkOnly = false;

    [SerializeField] public bool lockJumpInWalkOnly = false;
    [SerializeField] public bool lockDashInWalkOnly = false;

    [Header("Safe Ground Tracker")] [SerializeField]
    private float stableTimeThreshold = 0.2f;

    [SerializeField] private float maxHorizontalSpeed = 7f;
    [SerializeField] private LayerMask safeGroundMask;

    [Header("Landing Buffer")] [SerializeField]
    private bool enableLandingBuffer = true;

    [SerializeField, Tooltip("依落地衝擊決定恢復時長的下限/上限（秒）")]
    private Vector2 landingDurationRange = new Vector2(0.08f, 0.35f);

    [SerializeField, Tooltip("把落地瞬間 -Vy 映射為衝擊強度的最小/最大（m/s）")]
    private Vector2 landingImpactRange = new Vector2(3f, 16f);

    [SerializeField, Tooltip("落地後速度恢復曲線：x=時間(0~1), y=水平速度倍率(0~1)")]
    private AnimationCurve landingRecoveryCurve = AnimationCurve.EaseInOut(0f, 0.15f, 1f, 1f);

    [SerializeField, Tooltip("落地恢復是否可被 Dash 取消")]
    private bool cancelLandingBufferOnDash = true;

    [SerializeField, Tooltip("落地恢復是否可被跳躍取消")]
    private bool cancelLandingBufferOnJump = true;

    // ==========================================
    // 運行時狀態變數 (Runtime States)
    // ==========================================

    // Movement & Input States
    private Vector3 _rawInputMovement;
    private float _currentSpeed;
    private float _animSpeedParam = 0f;
    private bool _wasMoving = false;

    // Jump & Air States
    private int currentJumpCount = 0;
    private float _airPeakDownVel = 0f;
    private bool isOnGround = false;
    private bool wasOnGround = false;
    private RaycastHit _groundHit;
    private float _groundAngle = 999f;

    // Dash States
    private bool canDash = true;
    private bool isDashing = false;
    private bool _dashActive = false;
    private Vector3 _dashDir = Vector3.zero;

    // Wall & Environment States
    private bool _touchingWall = false;
    private Vector3 _wallNormal = Vector3.zero;
    private Collider currentCollider;
    private string currentSurface = "Default";

    // Interaction & Action States
    private bool isGrabbing;
    private bool isFinishClimb;
    private bool isPushing;

    // Safe Ground States
    private float stableTimer = 0f;
    private bool hasSafeGround = false;
    private Vector3 lastSafePos;
    private Quaternion lastSafeRot;

    // Landing Buffer States
    private bool _landingRecoverActive = false;
    private float _landingRecoverElapsed = 0f;
    private float _landingRecoverDuration = 0f;

    // VFX & Audio States
    private bool isFootstepPlaying = false;
    private bool hasPlayedGroundedVFX = false;
    private SFXType currentMoveState;

    [Header("Spirit Ref")]
    [SerializeField] private PangolinSpiritFollow spiritFollow1; // 在 Inspector 拉進去
    [SerializeField] private PangolinSpiritFollow spiritFollow2; // 在 Inspector 拉進去
    private bool _lastWeaponState = false;
    private float _weaponHoldTimer = 0f;
    private const float WeaponHoldDuration = 3f;
    private bool _isAiming;
    private bool _isHoldingWeapon;
    
    private EventBinding<OnPlayerDamaged> _binding;

    private void OnEnable()
    {
        _binding = new EventBinding<OnPlayerDamaged>(SetHurtAnimation);
        EventBus<OnPlayerDamaged>.Register(_binding);
    } 
    
    private void OnDisable()
    {
        if (_binding == null) return; //Optional
        EventBus<OnPlayerDamaged>.Deregister(_binding);
        _binding = null; //Optional
    }
    
    void SetHurtAnimation()
    {
        DoHitStop(0.1f);
        // 確保動畫與特效不受 HitStop 影響
        anim.updateMode = AnimatorUpdateMode.UnscaledTime; 
    
        anim.SetTrigger("Hurt");
        AudioManager.Instance.PlaySFX(SFXType.Hurt);
        RumbleManager.Instance.Rumble(0.7f, 0.9f, 0.1f);
    
        if (hurtVFX != null) 
        {
            // 粒子系統也需設置為 Unscaled 才能在停頓中播放
            var main = hurtVFX.main;
            main.useUnscaledTime = true;
            hurtVFX.Play();
        }
    }
    
    public void DoHitStop(float duration = 0.1f)
    {
        if (_isHitStopping) return;
        StartCoroutine(CoHitStop(duration));
    }

    private bool _isHitStopping;
    IEnumerator CoHitStop(float duration)
    {
        _isHitStopping = true;
        float originalScale = Time.timeScale;

        // 強制時間停止
        Time.timeScale = 0f;

        // 因為 TimeScale 為 0，必須使用 WaitForSecondsRealtime
        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = originalScale;
        _isHitStopping = false;
    }
    
    // ==========================================
    // Unity 生命週期
    // ==========================================
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();

        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.freezeRotation = true;

        if (noFrictionMaterial != null)
            _col.material = noFrictionMaterial;
    }

    private void Start()
    {
        input = PlayerInputHandler.Instance;
        if (input == null) Debug.LogError("沒有獲取 PlayerInputHandler");

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (stepVFX != null) stepVFX.Stop();
    }

    private void Update()
    {
        if (input.InputLock)
        {
            anim.Play("idle");
            if (stepVFX != null) stepVFX.Stop();
            AudioManager.Instance.StopSFXLoop();
            return;
        }

        Vector2 moveInput = input.MoveInput;
        bool isMovingInput = input.MoveInput.magnitude > 0.25f;

        _rawInputMovement = GetCameraRelativeMovement(moveInput);

        float targetSpeed = Mathf.Lerp(movementSpeed, runSpeed, input.MoveSpeedMultiplier);
        if (input.IsCollecting) targetSpeed *= 0.65f;
        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, 10f * Time.deltaTime);

        // --- 新增：處理 BlendTree 的本地速度參數 ---
        if (_isAiming || _weaponHoldTimer > 0f)
        {
            // 將世界坐標下的移動方向轉換為角色的本地坐標系
            Vector3 localMove = transform.InverseTransformDirection(_rawInputMovement);
        
            // 考慮目前的移動速度比例 (0~1)
            float speedFactor = (_rawInputMovement.magnitude < 0.1f) ? 0f : (_currentSpeed / runSpeed);
        
            float targetVX = localMove.x * speedFactor;
            float targetVZ = localMove.z * speedFactor;

            // 平滑更新 Animator 參數
            // 在 Update 中計算完 targetVX, targetVZ 後
            if (Mathf.Abs(targetVX) < 0.01f) targetVX = 0f;
            if (Mathf.Abs(targetVZ) < 0.01f) targetVZ = 0f;

            // 限制最大值，避免數值超出 BlendTree 定義範圍 (例如 -1 到 1)
            targetVX = Mathf.Clamp(targetVX, -1f, 1f);
            targetVZ = Mathf.Clamp(targetVZ, -1f, 1f);

            anim.SetFloat("velocityX", Mathf.Lerp(anim.GetFloat("velocityX"), targetVX, 10f * Time.deltaTime));
            anim.SetFloat("velocityZ", Mathf.Lerp(anim.GetFloat("velocityZ"), targetVZ, 10f * Time.deltaTime));
        }
        else
        {
            // 非瞄準狀態重置參數
            anim.SetFloat("velocityX", 0f);
            anim.SetFloat("velocityZ", 0f);
        }
        // ---------------------------------------
        
        SetWeaponAnimation();

        _animSpeedParam = (_rawInputMovement.magnitude < 0.1f)
            ? 0f
            : Mathf.Lerp(anim.GetFloat("Speed"), _rawInputMovement.magnitude * (targetSpeed / runSpeed),
                10f * Time.deltaTime);
        anim.SetFloat("Speed", _animSpeedParam);

        if (!PlayerInputHandler.Instance.InputLock && !isGrabbing)
        {
            if (!isWalkOnly || (isWalkOnly && !lockJumpInWalkOnly))
            {
                if (input.JumpPressed)
                {
                    if (cancelLandingBufferOnJump) _landingRecoverActive = false;
                    TryJump();
                    input.ResetJump();
                }
            }

            // 在 Update 內的動作處理區
            if (input.DashPressed)
            {
                // 檢查所有禁止 Dash 的條件
                bool canExecuteDash = !isDashing && 
                                      !_isHoldingWeapon && 
                                      (!isWalkOnly || (isWalkOnly && !lockDashInWalkOnly));

                if (canExecuteDash)
                {
                    if (cancelLandingBufferOnDash) _landingRecoverActive = false;
                    StartCoroutine(DashCoroutine());
                }

                // 無論是否成功執行，只要按下就重置，避免「存到下一幀」
                input.ResetDash();
            }
        }

        _wasMoving = isMovingInput;

        TrackSafeGround();
    }

    private void FixedUpdate()
    {
        if (PlayerInputHandler.Instance.InputLock)
        {
            _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
            return;
        }

        _touchingWall = false;
        _wallNormal = Vector3.zero;

        wasOnGround = isOnGround;
        isOnGround = IsGrounded(out _groundHit, out _groundAngle);

        if (!isOnGround)
        {
            _airPeakDownVel = Mathf.Min(_airPeakDownVel, _rb.linearVelocity.y);
        }

        if (isOnGround && !wasOnGround)
        {
            if (groundedVFX != null) groundedVFX.Play();
            ResetJump();
            hasPlayedGroundedVFX = true;

            if (enableLandingBuffer)
            {
                float impact = Mathf.InverseLerp(landingImpactRange.x, landingImpactRange.y, -_airPeakDownVel);
                _landingRecoverDuration = Mathf.Lerp(landingDurationRange.x, landingDurationRange.y, impact);
                _landingRecoverElapsed = 0f;
                _landingRecoverActive = _landingRecoverDuration > 0.0001f;
            }

            _airPeakDownVel = 0f;
        }
        else if (!isOnGround && wasOnGround)
        {
            hasPlayedGroundedVFX = false;
            _airPeakDownVel = 0f;
        }

        StepClimbCheck();

        if (_dashActive)
        {
            _rb.linearVelocity = new Vector3(_dashDir.x * dashSpeed, _rb.linearVelocity.y, _dashDir.z * dashSpeed);
        }
        else if (!isGrabbing)
        {
            if (!isDashing)
            {
                float landMul = 1f;
                if (_landingRecoverActive)
                {
                    _landingRecoverElapsed += Time.fixedDeltaTime;
                    float t = Mathf.Clamp01(_landingRecoverElapsed / _landingRecoverDuration);
                    landMul = Mathf.Clamp01(landingRecoveryCurve.Evaluate(t));

                    if (_landingRecoverElapsed >= _landingRecoverDuration)
                    {
                        _landingRecoverActive = false;
                        landMul = 1f;
                    }
                }

                ApplySchemeAMovement(landMul);

                if (_wasMoving && !_isAiming && _weaponHoldTimer <= 0) // 加入判斷
                {
                    var targetRot = Quaternion.LookRotation(_rawInputMovement);
                    var newRot = Quaternion.Slerp(_rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
                    _rb.MoveRotation(newRot);
                }
            }
        }

        if (isGrabbing)
        {
            // 保留
        }

        UpdateFootstepAudio();
    }

    // ==========================================
    // 核心移動邏輯
    // ==========================================
    private void ApplySchemeAMovement(float landMul)
    {
        Vector3 v = _rb.linearVelocity;
        Vector3 desiredHoriz = _rawInputMovement * (_currentSpeed * landMul);

        if (isOnGround)
        {
            Vector3 n = _groundHit.normal;
            Vector3 desiredOnGround = Vector3.ProjectOnPlane(desiredHoriz, n);
            Vector3 currentOnGround = Vector3.ProjectOnPlane(v, n);

            bool hasInput = _rawInputMovement.sqrMagnitude > 0.01f;

            if (hasInput)
            {
                Vector3 delta = desiredOnGround - currentOnGround;
                _rb.AddForce(delta * groundAccel, ForceMode.Acceleration);
            }
            else
            {
                _rb.AddForce(-currentOnGround * groundBrake, ForceMode.Acceleration);
            }

            if (v.y <= 0.01f)
                _rb.AddForce(-n * stickToGroundForce, ForceMode.Acceleration);
        }
        else
        {
            Vector3 currentHoriz = new Vector3(v.x, 0f, v.z);
            float horizSpeed = currentHoriz.magnitude;
            bool allowStick = _touchingWall && v.y >= wallMinUpVelToStick && horizSpeed <= wallReleaseSpeed;

            if (allowStick)
            {
                Vector3 wn = _wallNormal;
                wn.y = 0f;
                if (wn.sqrMagnitude > 0.0001f) wn.Normalize();

                Vector3 moveDir = _rawInputMovement;
                moveDir.y = 0f;
                if (moveDir.sqrMagnitude > 0.0001f) moveDir.Normalize();

                bool pressingWall = moveDir.sqrMagnitude > 0.0001f && Vector3.Dot(moveDir, -wn) > wallPressDotThreshold;

                desiredHoriz = Vector3.ProjectOnPlane(desiredHoriz, wn);
                currentHoriz = Vector3.ProjectOnPlane(currentHoriz, wn);

                Vector3 deltaAlongWall = desiredHoriz - currentHoriz;
                _rb.AddForce(deltaAlongWall * (airAccel + wallStickStrength), ForceMode.Acceleration);

                if (pressingWall)
                {
                    if (_rb.linearVelocity.y > 0f)
                    {
                        var vv = _rb.linearVelocity;
                        vv.y = 0f;
                        _rb.linearVelocity = vv;
                    }

                    if (_rb.linearVelocity.y > -wallForcedSlideSpeed)
                    {
                        var vv = _rb.linearVelocity;
                        vv.y = -wallForcedSlideSpeed;
                        _rb.linearVelocity = vv;
                    }
                }
                else
                {
                    if (_rb.linearVelocity.y < -wallSlideDownSpeed)
                    {
                        var vv = _rb.linearVelocity;
                        vv.y = -wallSlideDownSpeed;
                        _rb.linearVelocity = vv;
                    }
                }

                return;
            }

            Vector3 delta = desiredHoriz - currentHoriz;
            _rb.AddForce(new Vector3(delta.x, 0f, delta.z) * airAccel, ForceMode.Acceleration);
        }
    }

    private Vector3 GetCameraRelativeMovement(Vector2 cameraInput)
    {
        Vector3 f = cameraTransform.forward;
        f.y = 0f;
        Vector3 r = cameraTransform.right;
        r.y = 0f;
        return (f.normalized * cameraInput.y + r.normalized * cameraInput.x).normalized;
    }

    // ==========================================
    // 玩家行為 (Action)
    // ==========================================
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
            if (doubleJumpVFX != null) firstJumpVFX.Play();

        }
        else if (currentJumpCount == 2)
        {
            anim.SetBool("IsDoubleJump", true);
            anim.SetBool("Jump", false);
            AudioManager.Instance.PlaySFX(SFXType.Jump);
            if (doubleJumpVFX != null) doubleJumpVFX.Play();
        }

        var v = _rb.linearVelocity;
        v.y = jumpForce;
        _rb.linearVelocity = v;
    }

    private void ResetJump()
    {
        currentJumpCount = 0;
        anim.SetBool("Jump", false);
        anim.SetBool("IsDoubleJump", false);
    }

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
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
        }

        _dashActive = false;
        anim.SetBool("Dash", false);
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // ==========================================
    // 環境偵測與碰撞判斷
    // ==========================================
    private bool IsGrounded(out RaycastHit hit, out float angle)
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, Vector3.down, out hit, 0.35f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            angle = Vector3.Angle(hit.normal, Vector3.up);
            return angle <= maxGroundAngle;
        }

        angle = 999f;
        return false;
    }

    private bool IsGrounded()
    {
        return IsGrounded(out _, out _);
    }

    private void StepClimbCheck()
    {
        if (_rawInputMovement.sqrMagnitude < 0.0001f) return;

        Vector3 direction = _rawInputMovement.normalized;
        Vector3 lowerOrigin = transform.position + Vector3.up * 0.05f;
        if (Physics.Raycast(lowerOrigin, direction, out RaycastHit lowerHit, stepCheckDistance, groundLayer,
                QueryTriggerInteraction.Ignore))
        {
            Vector3 upperOrigin = transform.position + Vector3.up * maxStepHeight;
            if (!Physics.Raycast(upperOrigin, direction, stepCheckDistance, groundLayer,
                    QueryTriggerInteraction.Ignore))
            {
                _rb.MovePosition(_rb.position + Vector3.up * 0.1f);
            }
        }
    }

    private void TrackSafeGround()
    {
        if (!IsGrounded(out RaycastHit hit, out float angle))
        {
            stableTimer = 0f;
            return;
        }

        Vector3 horizontalVel = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        if (horizontalVel.magnitude > maxHorizontalSpeed)
        {
            stableTimer = 0f;
            return;
        }

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
                case "Wood": return "Wood";
                default: return "Default";
            }
        }

        return "Default";
    }

    // ==========================================
    // 物理碰撞事件
    // ==========================================
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
        if (other.gameObject.CompareTag("Object"))
        {
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

        float bestUpDot = 999f;
        Vector3 bestN = Vector3.zero;
        int count = other.contactCount;

        for (int i = 0; i < count; i++)
        {
            Vector3 n = other.GetContact(i).normal;
            float upDot = Mathf.Abs(Vector3.Dot(n, Vector3.up));
            if (upDot < bestUpDot)
            {
                bestUpDot = upDot;
                bestN = n;
            }
        }

        if (bestN != Vector3.zero)
        {
            if (bestUpDot <= wallNormalUpDotMax)
            {
                float angleFromUp = Vector3.Angle(bestN, Vector3.up);
                if (angleFromUp >= wallMinAngleFromUp)
                {
                    _touchingWall = true;
                    _wallNormal = bestN;
                }
            }
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

    // ==========================================
    // 音效、特效與動畫輔助
    // ==========================================
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

    private void SetWeaponAnimation()
    {
        // 判斷是否處於瞄準/射擊/收集狀態
        _isAiming = input.ShootPressed || input.IsCollecting;
    
        anim.SetBool("IsShooting", input.ShootPressed);
        anim.SetBool("IsCollecting", input.IsCollecting);
    
        if (_isAiming)
        {
            _weaponHoldTimer = WeaponHoldDuration;
        }

        if (_weaponHoldTimer > 0f)
        {
            _weaponHoldTimer -= Time.deltaTime;
        }

        _isHoldingWeapon = _weaponHoldTimer > 0f;

        // 處理角色轉向：瞄準時鎖定相機方向
        if (_isHoldingWeapon)
        {
            RotateTowardsCamera();
        }

        if (_isHoldingWeapon != _lastWeaponState)
        {
            _lastWeaponState = _isHoldingWeapon;
            spiritFollow1?.SetWeaponState(_isHoldingWeapon);
            spiritFollow2?.SetWeaponState(_isHoldingWeapon);
        }
    
        // 更新圖層權重：確保 AimMovement 圖層與 Shoot 圖層同步開啟
        SetAnimatorLayerWeight("Shoot", _isHoldingWeapon ? 1f : 0f);
        SetAnimatorLayerWeight("AimMovement", _isHoldingWeapon ? 1f : 0f);
    }

// 新增：強迫玩家轉向攝影機前方的方法
    private void RotateTowardsCamera()
    {
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0;
        if (camForward.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(camForward);
        
            // 計算角度差
            float angleDiff = Quaternion.Angle(_rb.rotation, targetRot);
        
            // 如果角度差很大（例如大於 90 度），給予額外的速度加成
            float speedMultiplier = angleDiff > 90f ? 1.5f : 1f;

            _rb.MoveRotation(Quaternion.Slerp(
                _rb.rotation, 
                targetRot, 
                aimTurnSpeed * speedMultiplier * Time.fixedDeltaTime
            ));
        }
    }
    
    public void SyncRotationToCameraInstant()
    {
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0;
        if (camForward.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(camForward);
            // 直接修改轉向，不經過 Slerp 插值
            _rb.rotation = targetRot;
            transform.rotation = targetRot; 
        }
    }
    
    private void SetAnimatorLayerWeight(string layerName, float weight)
    {
        int idx = anim.GetLayerIndex(layerName);
        if (idx != -1) anim.SetLayerWeight(idx, weight);
    }

    // ==========================================
    // 外部狀態修改
    // ==========================================
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
}