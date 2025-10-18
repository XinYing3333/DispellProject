using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 山神跟隨玩家的脚本。
/// </summary>
public class PangolinSpiritFollow : MonoBehaviour
{
    [Header("References")]
    public Transform player;        // 玩家
    public Transform followPoint;   // 玩家身上的空物件(可選)
    public Animator animator;       // 山神 Animator（需有 bool: isWalk）

    [Header("Trail Position (位移延遲)")]
    [Tooltip("儲存幀數（越多可支援更長延遲）")]
    public int trailBuffer = 30;
    [Tooltip("落後玩家的時間（秒）")]
    public float trailDelaySeconds = 0.35f;
    [Tooltip("位移的平滑時間常數（越小越跟得緊）")]
    public float positionSmooth = 0.1f;
    [Tooltip("沿玩家右側的輕微擺動幅度")]
    public float sideWobble = 0.3f;
    [Tooltip("基礎高度+呼吸起伏")]
    public float baseHeight = 1.0f;
    public float breatheAmp = 0.08f;
    public float breatheFreq = 1.3f;

    [Header("Facing Player (朝向延遲)")]
    [Tooltip("是否跟隨玩家朝向（Yaw）而非移動方向")]
    public bool facePlayerHeading = true;
    [Tooltip("落後玩家朝向的時間（秒）；建議與 trailDelaySeconds 接近")]
    public float rotationDelaySeconds = 0.25f;
    [Tooltip("旋轉速度（度/秒）")]
    public float turnSpeed = 360f;
    // 在 Fields 區加一個軸向修正參數（可在 Inspector 直接調）
    [Tooltip("修正模型前向：若模型朝 +X 請設 90；若朝 -Z 請設 180；若朝 -X 請設 -90")]
    public float yawOffsetDegrees = 0f;


    [Header("Movement Detect (動畫切換)")]
    public float moveThreshold = 0.02f; // 判斷移動的速度閾值（m/s）

    // --- internals ---
    private Vector3[] _posRing;
    private float[] _posTimeRing;
    private Quaternion[] _rotRing;
    private float[] _rotTimeRing;
    private int _ringIndex;
    private float _seed;
    private Vector3 _lastPos;

    private float timer;
    private string idleSubSM = "PangolinIdlePool";  // 子狀態機名稱
    private int idleCount = 2; //idle動畫數量
    private float minGap = 1.8f, maxGap = 4.0f;
    private float crossFade = 0.12f;
    private bool isWalk = false;

    void Awake()
    {
        _seed = Random.value * 100f;

        int n = Mathf.Max(12, trailBuffer);
        _posRing = new Vector3[n];
        _posTimeRing = new float[n];
        _rotRing = new Quaternion[n];
        _rotTimeRing = new float[n];

        Vector3 p = GetTargetBase();
        Quaternion r = player ? player.rotation : Quaternion.identity;

        for (int i = 0; i < n; i++)
        {
            _posRing[i] = p;
            _posTimeRing[i] = Time.time;
            _rotRing[i] = r;
            _rotTimeRing[i] = Time.time;
        }

        _lastPos = transform.position;
        if (_lastPos == Vector3.zero) _lastPos = p;
    }

    private void Update()
    {
        if(isWalk)return;
        /*if (isWalk)
        {
            sideWobble = 0.5f;
            breatheAmp = 0.5f;
            return;
        }
        else
        {
            sideWobble = 0.08f;
            breatheAmp = 0.08f;
        }*/
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            int idx = Random.Range(0, idleCount); // 上限對 int 是「不含」idleCount，OK
            string statePath = $"Base Layer.{idleSubSM}.PIdle_{idx}";
            int stateHash = Animator.StringToHash(statePath);

            if (animator.HasState(0, stateHash))
            {
                animator.CrossFade(stateHash, crossFade, 0, 0f);
            }
            else
            {
                Debug.LogWarning($"[PangolinSpiritFollow] Idle state not found: {statePath}");
                // 可選：回退到安全狀態
                // animator.CrossFade(Animator.StringToHash($"Base Layer.{idleSubSM}.PIdle_0"), crossFade, 0, 0f);
            }

            timer = Random.Range(minGap, maxGap);
        }
    }

    void LateUpdate()
    {
        if (!player) return;

        // ===== 1) 記錄「玩家基準點」與「玩家朝向」到 ring buffer =====
        _ringIndex = (_ringIndex + 1) % _posRing.Length;
        _posRing[_ringIndex] = GetTargetBase();
        _posTimeRing[_ringIndex] = Time.time;
        _rotRing[_ringIndex] = player.rotation;
        _rotTimeRing[_ringIndex] = Time.time;

        // ===== 2) 取得「trailDelaySeconds 前」的位置點 =====
        Vector3 lagPos = SamplePosAt(Time.time - trailDelaySeconds);

        // 輕微側擺 + 呼吸
        float t = Time.time + _seed;
        Vector3 right = Vector3.Cross(Vector3.up, player.forward).normalized;
        lagPos += right * Mathf.Sin(t * 2.1f) * sideWobble;
        lagPos.y = (followPoint ? followPoint.position.y : player.position.y) 
                   + baseHeight + Mathf.Sin(t * breatheFreq) * breatheAmp;

        // 位置平滑（指數平滑 / time constant）
        float posAlpha = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, positionSmooth));
        transform.position = Vector3.Lerp(transform.position, lagPos, posAlpha);

        // ===== 3) 取得「rotationDelaySeconds 前」的朝向，僅保留 Yaw =====
        Quaternion lagRot = SampleRotAt(Time.time - rotationDelaySeconds);
        if (facePlayerHeading)
        {
            Vector3 yawFwd = FlattenToYaw(lagRot * Vector3.forward);
            if (yawFwd.sqrMagnitude > 1e-4f)
            {
                Quaternion want = Quaternion.LookRotation(yawFwd, Vector3.up)
                                  * Quaternion.Euler(0f, yawOffsetDegrees, 0f); // ← 軸向修正

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, want, turnSpeed * Time.deltaTime);
            }
        }

        // ===== 4) Animator：isWalk =====
        float moveSpeed = (transform.position - _lastPos).magnitude / Mathf.Max(Time.deltaTime, 1e-5f);
        bool isMoving = moveSpeed > moveThreshold;
        if (animator) animator.SetBool("isWalk", isMoving);
        isWalk = isMoving;
        _lastPos = transform.position;
    }

    // 取某個時間點的延遲位置（線性插值兩個鄰近節點）
    Vector3 SamplePosAt(float targetTime)
    {
        int n = _posRing.Length;
        // 從最新往回找第一個時間 <= targetTime 的樣本
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
                // b 用上一筆（時間較新）
                int idxNext = (idx + 1) % n;
                b = _posRing[idxNext]; tb = _posTimeRing[idxNext];
                break;
            }
        }
        float u = Mathf.InverseLerp(ta, tb, targetTime);
        return Vector3.Lerp(a, b, Mathf.Clamp01(u));
    }

    // 取某個時間點的延遲朝向（Slerp 兩個鄰近節點）
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
        return player.position + Vector3.up * 0.0f; // 原點，再由 baseHeight 提升
    }

    // 只保留地面水平的朝向
    static Vector3 FlattenToYaw(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GetTargetBase() + Vector3.up * baseHeight, 0.1f);
    }
#endif
}
