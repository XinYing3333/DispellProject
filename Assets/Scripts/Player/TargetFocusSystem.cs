using System;
using UnityEngine;
using Cinemachine;
using Player;

[ExecuteAlways]
public class TargetFocusSystem : MonoBehaviour
{
    [Header("Refs")]
    public CinemachineFreeLook freeLook;    // FreeLook 攝影機
    public Transform target;                // 目標（外部點）
    [Min(0f)] public float playerRadius = 2.0f; // 玩家周圍圓半徑（XZ 平面）

    [Header("Lock Settings")]
    public float lockSeconds = 2.0f;        // 鎖定維持時間（秒）
    [SerializeField] private float cameraOffset = 3f; // 相機在 B 點上方的高度

    // 狀態
    private Transform _tempFollow;                    // 暫時 pivot
    private Transform _origFollow;                    // 原始 Follow
    private Transform _origLookAt;                    // 原始 LookAt
    private bool _locking;
    private float _timer;
    private bool _hasPressedSinceLastLock;

    // 備份 FreeLook 軸速度與 recenter
    private float _origXSpeed, _origYSpeed;
    private bool  _origHeadRecenteringEnabled;
    private float _origHeadRecenteringTime, _origHeadWait;
    private bool  _origYRecenteringEnabled;
    private float _origYRecenteringTime, _origYWait;

    // ★ 備份 Orbits(Height/Radius)
    private float[] _origHeights = new float[3];
    private float[] _origRadii   = new float[3];

    // 除錯可視化
    private bool _hasPoints;
    private Vector3 _pointC;
    private Vector3 _pointB;
    private Vector3 _followVel;


    void Awake()
    {
        if (!freeLook) Debug.LogWarning("FreeLook 未指定！");
        if (freeLook)
        {
            _origFollow = freeLook.Follow;
            _origLookAt = freeLook.LookAt;

            _origXSpeed = freeLook.m_XAxis.m_MaxSpeed;
            _origYSpeed = freeLook.m_YAxis.m_MaxSpeed;

            _origHeadRecenteringEnabled = freeLook.m_RecenterToTargetHeading.m_enabled;
            _origHeadRecenteringTime    = freeLook.m_RecenterToTargetHeading.m_RecenteringTime;
            _origHeadWait               = freeLook.m_RecenterToTargetHeading.m_WaitTime;

            _origYRecenteringEnabled = freeLook.m_YAxisRecentering.m_enabled;
            _origYRecenteringTime    = freeLook.m_YAxisRecentering.m_RecenteringTime;
            _origYWait               = freeLook.m_YAxisRecentering.m_WaitTime;
        }
    }

    void OnDisable()
    {
        if (_locking) RestoreAll();
    }

    void Update()
    {
        bool pressed = PlayerInputHandler.Instance.IsTargetPressed;

        // 按下瞬間執行一次
        if (pressed && !_locking && !_hasPressedSinceLastLock)
        {
            StartLock();
            _hasPressedSinceLastLock = true;
        }
        else if (!pressed)
        {
            _hasPressedSinceLastLock = false;
        }

        if (_locking)
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer >= lockSeconds)
            {
                EndLock();
                return;
            }

            // 鎖定期間持續刷新相機位置
            if (target != null && freeLook != null && _tempFollow != null)
            {
                _hasPoints = ComputeIntersectionsXZ(transform.position, target.position, playerRadius,
                    out _pointC, out _pointB);
                if (_hasPoints)
                {
                    Vector3 offsetPos = _pointB + Vector3.up * cameraOffset;
                    _tempFollow.position = offsetPos; // FreeLook Orbits=0 → 鏡頭會貼這個點
                }
            }
        }
        else
        {
            // 非鎖定時也計算（方便在編輯器預覽）
            _hasPoints = ComputeIntersectionsXZ(transform.position,
                target ? target.position : transform.position + Vector3.forward * 3f,
                playerRadius, out _pointC, out _pointB);
        }
    }

    // ===== 進入鎖定 =====
    void StartLock()
    {
        if (!target || !freeLook) return;
        _locking = true;
        _timer = 0f;

        if (!ComputeIntersectionsXZ(transform.position, target.position, playerRadius,
            out _pointC, out _pointB))
            return;

        // 建立暫時 pivot
        if (_tempFollow == null)
        {
            var go = new GameObject("[CameraLockPivot]");
            go.hideFlags = HideFlags.HideAndDontSave;
            _tempFollow = go.transform;
        }

        _tempFollow.position = _pointB + Vector3.up * cameraOffset;

        // ★ 備份並把三個 Orbits 清零（Height/Radius）
        for (int i = 0; i < 3; i++)
        {
            var o = freeLook.m_Orbits[i];      // Orbit 是 struct，要用暫存再回寫
            _origHeights[i] = o.m_Height;
            _origRadii[i]   = o.m_Radius;
            o.m_Height = 0f;
            o.m_Radius = 0f;
            freeLook.m_Orbits[i] = o;          // ← 回寫很重要，否則 Inspector 看不到變化
        }

        // 設置 Follow / LookAt
        _origFollow = freeLook.Follow;
        _origLookAt = freeLook.LookAt;
        freeLook.Follow = _tempFollow;
        freeLook.LookAt = target;

        // 鎖住玩家相機輸入
        _origXSpeed = freeLook.m_XAxis.m_MaxSpeed;
        _origYSpeed = freeLook.m_YAxis.m_MaxSpeed;
        freeLook.m_XAxis.m_MaxSpeed = 0f;
        freeLook.m_YAxis.m_MaxSpeed = 0f;

        // 關閉 recenter
        _origHeadRecenteringEnabled = freeLook.m_RecenterToTargetHeading.m_enabled;
        _origYRecenteringEnabled    = freeLook.m_YAxisRecentering.m_enabled;
        freeLook.m_RecenterToTargetHeading.m_enabled = false;
        freeLook.m_YAxisRecentering.m_enabled       = false;
    }

    // ===== 結束鎖定 =====
    void EndLock()
    {
        _locking = false;
        RestoreAll();
    }

    // ===== 還原所有 =====
    void RestoreAll()
    {
        if (!freeLook) return;

        // 還原 Follow / LookAt
        freeLook.Follow = _origFollow;
        freeLook.LookAt = _origLookAt;

        // 還原軸速度
        freeLook.m_XAxis.m_MaxSpeed = _origXSpeed;
        freeLook.m_YAxis.m_MaxSpeed = _origYSpeed;

        // 還原 recenter
        freeLook.m_RecenterToTargetHeading.m_enabled = _origHeadRecenteringEnabled;
        freeLook.m_RecenterToTargetHeading.m_RecenteringTime = _origHeadRecenteringTime;
        freeLook.m_RecenterToTargetHeading.m_WaitTime        = _origHeadWait;

        freeLook.m_YAxisRecentering.m_enabled = _origYRecenteringEnabled;
        freeLook.m_YAxisRecentering.m_RecenteringTime = _origYRecenteringTime;
        freeLook.m_YAxisRecentering.m_WaitTime        = _origYWait;

        // ★ 還原 Orbits(Height/Radius)
        for (int i = 0; i < 3; i++)
        {
            var o = freeLook.m_Orbits[i];
            o.m_Height = _origHeights[i];
            o.m_Radius = _origRadii[i];
            freeLook.m_Orbits[i] = o;
        }
    }

    // ======== 計算交點（XZ 平面） ========
    static bool ComputeIntersectionsXZ(Vector3 playerPos3, Vector3 targetPos3, float r,
        out Vector3 pointC, out Vector3 pointB)
    {
        Vector2 C = new Vector2(playerPos3.x, playerPos3.z);
        Vector2 P0 = new Vector2(targetPos3.x, targetPos3.z);
        Vector2 P1 = new Vector2(playerPos3.x, playerPos3.z);
        Vector2 d = (P1 - P0);
        float a = d.sqrMagnitude;

        pointC = pointB = default;
        if (a < 1e-10f) return false;

        Vector2 f = P0 - C;
        float b = 2f * Vector2.Dot(d, f);
        float c = Vector2.Dot(f, f) - r * r;
        float disc = b * b - 4f * a * c;
        if (disc < 0f) return false;

        float sqrtD = Mathf.Sqrt(Mathf.Max(0f, disc));
        float t1 = (-b - sqrtD) / (2f * a);
        float t2 = (-b + sqrtD) / (2f * a);

        Vector2 I1 = P0 + t1 * d;
        Vector2 I2 = P0 + t2 * d;
        Vector2 Cxz, Bxz;
        if (t1 < t2) { Cxz = I1; Bxz = I2; } else { Cxz = I2; Bxz = I1; }

        float y = playerPos3.y;
        pointC = new Vector3(Cxz.x, y, Cxz.y);
        pointB = new Vector3(Bxz.x, y, Bxz.y);
        return true;
    }

    // ======== Gizmos 可視化 ========
    void OnDrawGizmos()
    {
        if (!_hasPoints) return;

        Vector3 playerPos = transform.position;
        Vector3 targetPos = target ? target.position : playerPos + Vector3.forward * 3f;

        // 玩家範圍
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.8f);
        Gizmos.DrawWireSphere(playerPos, playerRadius);

        // 玩家 → 目標線
        Gizmos.color = Color.red;
        Gizmos.DrawLine(playerPos, targetPos);

        // 目標點
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(targetPos, 0.08f);

        // Player
        Gizmos.color = Color.gray;
        Gizmos.DrawSphere(playerPos, 0.08f);

        // PointC (靠近 target)
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(_pointC, 0.12f);

        // PointB (另一側)
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(_pointB, 0.12f);

        // B → 上方 offset
        Vector3 bTop = _pointB + Vector3.up * cameraOffset;
        Gizmos.color = new Color(0.6f, 0f, 1f, 0.8f);
        Gizmos.DrawLine(_pointB, bTop);
        Gizmos.DrawSphere(bTop, 0.12f);

        // 連線 Target→C→B
        Gizmos.color = new Color(0f, 1f, 0.3f, 0.6f);
        Gizmos.DrawLine(targetPos, _pointC);
        Gizmos.DrawLine(_pointC, _pointB);

        // 若有攝影機 → 畫出 pivot 位置
        if (freeLook && _tempFollow)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(_tempFollow.position, 0.1f);
            Gizmos.DrawLine(_pointB, _tempFollow.position);
        }
    }
}
