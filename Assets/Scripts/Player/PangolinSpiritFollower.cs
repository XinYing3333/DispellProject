using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 山神跟隨玩家的脚本。
/// </summary>
public class PangolinSpiritFollow : MonoBehaviour
{
    [Header("References")]
    public Transform player;        
    public Transform followPoint;   
    public Animator animator;       
    public ParticleSystem switchVFX;       
    public ParticleSystem switchBackVFX;       

    [Header("Trail Position (位移延遲)")]
    public int trailBuffer = 30;
    public float trailDelaySeconds = 0.35f;
    public float positionSmooth = 0.1f;
    public float sideWobble = 0.3f;
    public float baseHeight = 1.0f;
    public float breatheAmp = 0.08f;
    public float breatheFreq = 1.3f;

    [Header("Facing Player (朝向延遲)")]
    public bool facePlayerHeading = true;
    public float rotationDelaySeconds = 0.25f;
    public float turnSpeed = 360f;
    public float yawOffsetDegrees = 0f;

    [Header("Movement Detect (動畫切換)")]
    public float moveThreshold = 0.02f; 

    // --- internals ---
    private Vector3[] _posRing;
    private float[] _posTimeRing;
    private Quaternion[] _rotRing;
    private float[] _rotTimeRing;
    private int _ringIndex;
    private float _seed;
    private Vector3 _lastPos;

    private float timer;
    private string idleSubSM = "PangolinIdlePool";  
    private int idleCount = 2; 
    private float minGap = 1.8f, maxGap = 4.0f;
    private float crossFade = 0.12f;
    
    private bool isWalk = false;
    private bool isTransformed = false; // 新增：是否處於變身狀態

    void Awake()
    {
        _seed = Random.value * 100f;

        int n = Mathf.Max(12, trailBuffer);
        _posRing = new Vector3[n];
        _posTimeRing = new float[n];
        _rotRing = new Quaternion[n];
        _rotTimeRing = new float[n];

        ResetTrailBuffer();
    }

    private void Update()
    {
        // 若處於變身狀態或移動中，不執行閒置動畫邏輯
        if (isWalk || isTransformed) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            int idx = Random.Range(0, idleCount); 
            string statePath = $"Base Layer.{idleSubSM}.PIdle_{idx}";
            int stateHash = Animator.StringToHash(statePath);

            if (animator.HasState(0, stateHash))
            {
                animator.CrossFade(stateHash, crossFade, 0, 0f);
            }
            else
            {
                Debug.LogWarning($"[PangolinSpiritFollow] Idle state not found: {statePath}");
            }

            timer = Random.Range(minGap, maxGap);
        }
    }

    void LateUpdate()
    {
        if (!player) return;

        _ringIndex = (_ringIndex + 1) % _posRing.Length;
        _posRing[_ringIndex] = GetTargetBase();
        _posTimeRing[_ringIndex] = Time.time;
        _rotRing[_ringIndex] = player.rotation;
        _rotTimeRing[_ringIndex] = Time.time;

        Vector3 lagPos = SamplePosAt(Time.time - trailDelaySeconds);

        float t = Time.time + _seed;
        Vector3 right = Vector3.Cross(Vector3.up, player.forward).normalized;
        lagPos += right * Mathf.Sin(t * 2.1f) * sideWobble;
        lagPos.y = (followPoint ? followPoint.position.y : player.position.y) 
                   + baseHeight + Mathf.Sin(t * breatheFreq) * breatheAmp;

        float posAlpha = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, positionSmooth));
        transform.position = Vector3.Lerp(transform.position, lagPos, posAlpha);

        Quaternion lagRot = SampleRotAt(Time.time - rotationDelaySeconds);
        if (facePlayerHeading)
        {
            Vector3 yawFwd = FlattenToYaw(lagRot * Vector3.forward);
            if (yawFwd.sqrMagnitude > 1e-4f)
            {
                Quaternion want = Quaternion.LookRotation(yawFwd, Vector3.up)
                                  * Quaternion.Euler(0f, yawOffsetDegrees, 0f); 

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, want, turnSpeed * Time.deltaTime);
            }
        }

        float moveSpeed = (transform.position - _lastPos).magnitude / Mathf.Max(Time.deltaTime, 1e-5f);
        bool isMoving = moveSpeed > moveThreshold;
        if (animator) animator.SetBool("isWalk", isMoving);
        isWalk = isMoving;
        _lastPos = transform.position;
    }

    /// <summary>
    /// 供外部呼叫：切換武器狀態
    /// </summary>
    public void SetWeaponState(bool isUsingWeapon)
    {
        if (isTransformed == isUsingWeapon) return;
        isTransformed = isUsingWeapon;

        if (isTransformed)
        {
            // 觸發變身動畫 (需在 Animator 中設定對應 Trigger)
            if (animator) animator.Play("pangolin-BALL");
            if (switchVFX) switchVFX.Play();
        }
        else
        {
            // 觸發還原動畫
            if (switchBackVFX) switchBackVFX.Play();
            if (animator) animator.Play("pangolin-SWIM2");
        }
    }

    /// <summary>
    /// 重置軌跡陣列
    /// </summary>
    private void ResetTrailBuffer()
    {
        Vector3 p = GetTargetBase();
        Quaternion r = player ? player.rotation : Quaternion.identity;

        for (int i = 0; i < _posRing.Length; i++)
        {
            _posRing[i] = p;
            _posTimeRing[i] = Time.time;
            _rotRing[i] = r;
            _rotTimeRing[i] = Time.time;
        }

        _lastPos = transform.position;
        if (_lastPos == Vector3.zero) _lastPos = p;
    }

    Vector3 SamplePosAt(float targetTime)
    {
        int n = _posRing.Length;
        Vector3 a = _posRing[_ringIndex];
        Vector3 b = a;
        float ta = _posTimeRing[_ringIndex];
        float tb = ta;

        for (int i = 0; i < n; i++)
        {
            int idx = (_ringIndex - i + n) % n;
            if (_posTimeRing[idx] <= targetTime)
            {
                a = _posRing[idx]; ta = _posTimeRing[idx];
                int idxNext = (idx + 1) % n;
                b = _posRing[idxNext]; tb = _posTimeRing[idxNext];
                break;
            }
        }
        float u = Mathf.InverseLerp(ta, tb, targetTime);
        return Vector3.Lerp(a, b, Mathf.Clamp01(u));
    }

    Quaternion SampleRotAt(float targetTime)
    {
        int n = _rotRing.Length;
        Quaternion a = _rotRing[_ringIndex];
        Quaternion b = a;
        float ta = _rotTimeRing[_ringIndex];
        float tb = ta;

        for (int i = 0; i < n; i++)
        {
            int idx = (_ringIndex - i + n) % n;
            if (_rotTimeRing[idx] <= targetTime)
            {
                a = _rotRing[idx]; ta = _rotTimeRing[idx];
                int idxNext = (idx + 1) % n;
                b = _rotRing[idxNext]; tb = _rotTimeRing[idxNext];
                break;
            }
        }
        float u = Mathf.InverseLerp(ta, tb, targetTime);
        return Quaternion.Slerp(a, b, Mathf.Clamp01(u));
    }

    Vector3 GetTargetBase()
    {
        if (followPoint) return followPoint.position;
        return player.position + Vector3.up * 0.0f; 
    }

    static Vector3 FlattenToYaw(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GetTargetBase() + Vector3.up * baseHeight, 0.1f);
    }
#endif
}