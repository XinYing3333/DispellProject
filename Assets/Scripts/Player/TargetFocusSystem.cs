using System;
using UnityEngine;
using Cinemachine;
using DefaultNamespace.EventBus.Events.UI;
using Player;

[ExecuteAlways]
public class TargetFocusSystem : MonoBehaviour
{
    [Header("Refs")]
    public CinemachineFreeLook freeLook;    
    public Transform target;                
    [Min(0f)] public float playerRadius = 2.0f; 

    [Header("Lock Settings")]
    public float lockSeconds = 2.0f;        
    [SerializeField] private float cameraOffset = 3f; 
    
    [Header("UI Target Marker")]
    [SerializeField] private RectTransform iconPrefab;
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private Vector3 uiWorldOffset = new Vector3(0, 1.5f, 0);
    [SerializeField] private float screenEdgePadding = 24f;

// 顯示時間 & 淡出時間
    [SerializeField] private float uiShowDuration = 4f;
    [SerializeField] private float uiFadeDuration = 0.6f;

    private RectTransform _iconInstance;
    private CanvasGroup _iconCanvasGroup;
    private Camera _cam;
    private float _uiTimer;
    private bool _uiFading;
    
    // 狀態
    private Transform _tempFollow;                    
    private Transform _origFollow;                    
    private Transform _origLookAt;                    
    private bool _locking;
    private float _timer;
    private bool _hasPressedSinceLastLock;

    // 備份 FreeLook 軸速度與 recenter
    private float _origXSpeed, _origYSpeed;
    private bool  _origHeadRecenteringEnabled;
    private float _origHeadRecenteringTime, _origHeadWait;
    private bool  _origYRecenteringEnabled;
    private float _origYRecenteringTime, _origYWait;

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
    
    void Start()
    {
        if (!Application.isPlaying) return;

        _cam = Camera.main;

        if (iconPrefab && uiCanvas)
        {
            _iconInstance = Instantiate(iconPrefab, uiCanvas.transform);
            _iconInstance.gameObject.SetActive(false);

            // 加入 CanvasGroup 控制透明度
            _iconCanvasGroup = _iconInstance.GetComponent<CanvasGroup>();
            if (!_iconCanvasGroup)
                _iconCanvasGroup = _iconInstance.gameObject.AddComponent<CanvasGroup>();
            _iconCanvasGroup.alpha = 0f;
        }
    }



    void Update()
    {
        // 🔒 在編輯器模式下，只更新 Gizmo 幾何資訊，不執行輸入邏輯
        if (!Application.isPlaying)
        {
            _hasPoints = ComputeIntersectionsXZ(transform.position,
                target ? target.position : transform.position + Vector3.forward * 3f,
                playerRadius, out _pointC, out _pointB);
            return;
        }
        
        if (Application.isPlaying && _iconInstance && target)
        {
            UpdateTargetUI();
        }

        bool pressed = PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.IsTargetPressed;

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

            if (target != null && freeLook != null && _tempFollow != null)
            {
                _hasPoints = ComputeIntersectionsXZ(transform.position, target.position, playerRadius,
                    out _pointC, out _pointB);
                if (_hasPoints)
                {
                    Vector3 offsetPos = _pointB + Vector3.up * cameraOffset;
                    _tempFollow.position = offsetPos; 
                }
            }
        }
        // --- UI 顯示控制 ---
        if (Application.isPlaying && _iconInstance && target)
        {
            UpdateTargetUI();
            UpdateTargetUITimer();
        }
    }

    void StartLock()
    {
        if (!target || !freeLook) return;
        _locking = true;
        _timer = 0f;

        if (!ComputeIntersectionsXZ(transform.position, target.position, playerRadius,
            out _pointC, out _pointB))
            return;

        if (_tempFollow == null)
        {
            var go = new GameObject("[CameraLockPivot]");
            go.hideFlags = HideFlags.HideAndDontSave;
            _tempFollow = go.transform;
        }

        _tempFollow.position = _pointB + Vector3.up * cameraOffset;

        for (int i = 0; i < 3; i++)
        {
            var o = freeLook.m_Orbits[i];
            _origHeights[i] = o.m_Height;
            _origRadii[i]   = o.m_Radius;
            o.m_Height = 0f;
            o.m_Radius = 0f;
            freeLook.m_Orbits[i] = o;
        }

        _origFollow = freeLook.Follow;
        _origLookAt = freeLook.LookAt;
        freeLook.Follow = _tempFollow;
        freeLook.LookAt = target;

        _origXSpeed = freeLook.m_XAxis.m_MaxSpeed;
        _origYSpeed = freeLook.m_YAxis.m_MaxSpeed;
        freeLook.m_XAxis.m_MaxSpeed = 0f;
        freeLook.m_YAxis.m_MaxSpeed = 0f;

        _origHeadRecenteringEnabled = freeLook.m_RecenterToTargetHeading.m_enabled;
        _origYRecenteringEnabled    = freeLook.m_YAxisRecentering.m_enabled;
        freeLook.m_RecenterToTargetHeading.m_enabled = false;
        freeLook.m_YAxisRecentering.m_enabled       = false;
        
        // 顯示目標 UI
        if (_iconInstance)
        {
            _uiTimer = 0f;
            _uiFading = false;
            _iconInstance.gameObject.SetActive(true);
            _iconCanvasGroup.alpha = 1f; // 立刻顯示
        }
        EventBus<RevealObjective>.Raise(new RevealObjective());
    }

    void EndLock()
    {
        _locking = false;
        RestoreAll();
    }

    void RestoreAll()
    {
        if (!freeLook) return;

        freeLook.Follow = _origFollow;
        freeLook.LookAt = _origLookAt;

        freeLook.m_XAxis.m_MaxSpeed = _origXSpeed;
        freeLook.m_YAxis.m_MaxSpeed = _origYSpeed;

        freeLook.m_RecenterToTargetHeading.m_enabled = _origHeadRecenteringEnabled;
        freeLook.m_RecenterToTargetHeading.m_RecenteringTime = _origHeadRecenteringTime;
        freeLook.m_RecenterToTargetHeading.m_WaitTime        = _origHeadWait;

        freeLook.m_YAxisRecentering.m_enabled = _origYRecenteringEnabled;
        freeLook.m_YAxisRecentering.m_RecenteringTime = _origYRecenteringTime;
        freeLook.m_YAxisRecentering.m_WaitTime        = _origYWait;

        for (int i = 0; i < 3; i++)
        {
            var o = freeLook.m_Orbits[i];
            o.m_Height = _origHeights[i];
            o.m_Radius = _origRadii[i];
            freeLook.m_Orbits[i] = o;
        }
        EventBus<HideObjective>.Raise(new HideObjective());
    }
    
    void UpdateTargetUI()
    {
        if (!_cam || !target || !_iconInstance) return;

        Vector3 screenPos = _cam.WorldToScreenPoint(target.position);

        // --- 如果目標在鏡頭後面 ---
        if (screenPos.z < 0)
        {
            // 鏡射到前方（讓方向正確）
            screenPos *= -1;
            screenPos.z = 0;
        }

        // --- 限制 icon 不會超出螢幕邊界 ---
        float edgeOffset = 50f; // 距離螢幕邊緣留白（可調）
        screenPos.x = Mathf.Clamp(screenPos.x, edgeOffset, Screen.width - edgeOffset);
        screenPos.y = Mathf.Clamp(screenPos.y, edgeOffset, Screen.height - edgeOffset);

        // 套用到 RectTransform
        _iconInstance.position = screenPos;
    }


    void UpdateTargetUITimer()
    {
        if (_uiFading) return;

        _uiTimer += Time.deltaTime;

        if (_uiTimer >= uiShowDuration)
        {
            StartCoroutine(FadeOutIcon());
            _uiFading = true;
        }
    }

    System.Collections.IEnumerator FadeOutIcon()
    {
        float t = 0f;
        float startAlpha = _iconCanvasGroup.alpha;

        while (t < uiFadeDuration)
        {
            t += Time.deltaTime;
            _iconCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / uiFadeDuration);
            yield return null;
        }

        _iconCanvasGroup.alpha = 0f;
        _iconInstance.gameObject.SetActive(false);
    }


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

    // ✅ 可視化：即使在編輯模式也能看
    void OnDrawGizmos()
    {
        if (!_hasPoints) return;

        Vector3 playerPos = transform.position;
        Vector3 targetPos = target ? target.position : playerPos + Vector3.forward * 3f;

        Gizmos.color = new Color(1f, 0.85f, 0f, 0.8f);
        Gizmos.DrawWireSphere(playerPos, playerRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(playerPos, targetPos);

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(targetPos, 0.08f);

        Gizmos.color = Color.gray;
        Gizmos.DrawSphere(playerPos, 0.08f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(_pointC, 0.12f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(_pointB, 0.12f);

        Vector3 bTop = _pointB + Vector3.up * cameraOffset;
        Gizmos.color = new Color(0.6f, 0f, 1f, 0.8f);
        Gizmos.DrawLine(_pointB, bTop);
        Gizmos.DrawSphere(bTop, 0.12f);

        Gizmos.color = new Color(0f, 1f, 0.3f, 0.6f);
        Gizmos.DrawLine(targetPos, _pointC);
        Gizmos.DrawLine(_pointC, _pointB);

        if (freeLook && _tempFollow)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(_tempFollow.position, 0.1f);
            Gizmos.DrawLine(_pointB, _tempFollow.position);
        }
    }
}
