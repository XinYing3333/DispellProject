using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // 若用舊 Input，移除這行並改成 KeyCode 判斷
using Cinemachine;
using UnityEngine.UI;

public class TargetFocusSystem : MonoBehaviour
{
    [Header("Refs")]
    public CinemachineFreeLook freeLook;   // 你的 FreeLook 相機
    public Transform player;               // 玩家
    public Transform target;               // 目標點（任務目標、互動點等）
    public Camera gameCamera;              // 一般填主相機
    public RectTransform markerUI;         // 畫面上的圖示（Image 的 RectTransform）

    [Header("Focus Trigger")]
    public KeyCode focusKey = KeyCode.T;   // 觸發鍵（可換成你自己的 Input）
    public float focusHoldTime = 1.2f;     // 單次聚焦維持秒數（長按會持續）

    [Header("LookAt Helper")]
    public bool useLookAtHelper = true;    // 使用輔助點讓運鏡更自然
    public float helperDistance = 6f;      // 輔助點離玩家距離
    public float helperHeight = 1.5f;      // 輔助點高度微調
    public float helperTargetBlend = 0.25f;// 目標高度混合(0=跟玩家高,1=跟目標高)

    [Header("FreeLook Recentering (聚焦時啟用)")]
    public bool enableHeadingRecentering = true;
    public float headingRecenteringTime = 0.35f; // 越小越快
    public float headingRecenteringWait = 0f;

    public bool enableYAxisRecentering = true;
    public float yRecenteringTime = 0.35f;
    public float yRecenteringWait = 0f;
    public float focusYHeight = 0.45f; // 0~1：聚焦時想要的仰角(越小越低)

    [Header("Smooth Turn")]
    public bool smoothTurn = true;
    [Tooltip("最大平滑旋轉速度(度/秒)")]
    public float turnSpeedDegPerSec = 360f;
    [Tooltip("越小越跟手；0.10~0.30常用")]
    public float turnEase = 0.18f;
    private float _xVel; // SmoothDampAngle 速度暫存

    [Header("Smooth Y (可選)")]
    public bool smoothY = false;
    public float desiredYOnFocus = 0.45f; // 0~1，FreeLook 的 Y slider
    public float ySmoothTime = 0.20f;
    private float _yVel;

    
    [Header("Marker UI")]
    public Vector3 markerWorldOffset = new Vector3(0, 1.5f, 0);
    [Range(8f, 48f)] public float screenEdgePadding = 24f;
    public bool rotateOffscreenArrow = true;

    private Transform _origLookAt;
    private Transform _origFollow;

    [Header("Restore Options")]
    public bool restoreLookAtOnStop = true;   // 預設還原 LookAt
    public bool restoreFollowOnStop = false;  // 如聚焦時有改 Follow 才開
    
    // --- private
    private Transform _helper;
    private bool _focusing;
    private float _timer;

    // 備份原本 recenter 設定
    private bool _origHeadEnable;
    private float _origHeadTime, _origHeadWait;
    private bool _origYEnable;
    private float _origYTime, _origYWait;

    void Awake()
    {
        if (!gameCamera) gameCamera = Camera.main;

        if (useLookAtHelper)
        {
            _helper = new GameObject("FreeLook_FocusHelper").transform;
            _helper.hideFlags = HideFlags.HideInHierarchy;
        }

        if (markerUI) markerUI.gameObject.SetActive(false);

        // 備份 FreeLook 原設定（避免影響你平常運鏡習慣）
        if (freeLook != null)
        {
            _origHeadEnable = freeLook.m_RecenterToTargetHeading.m_enabled;
            _origHeadTime   = freeLook.m_RecenterToTargetHeading.m_RecenteringTime;
            _origHeadWait   = freeLook.m_RecenterToTargetHeading.m_WaitTime;

            _origYEnable = freeLook.m_YAxisRecentering.m_enabled;
            _origYTime   = freeLook.m_YAxisRecentering.m_RecenteringTime;
            _origYWait   = freeLook.m_YAxisRecentering.m_WaitTime;
            
            _origLookAt = freeLook.LookAt;
            _origFollow = freeLook.Follow;
        }
    }

    void OnDisable()
    {
        if (_focusing) StopFocus(); // 確保退出聚焦
        // 再保險一次：如果使用者在外部關掉腳本也能回復
        if (freeLook != null)
        {
            if (restoreLookAtOnStop) freeLook.LookAt = _origLookAt;
           // if (restoreFollowOnStop) freeLook.Follow = _origFollow;
        }
    }
    
    void OnDestroy()
    {
        if (_helper) Destroy(_helper.gameObject);
    }

    void Update()
    {
        if (freeLook == null || player == null || target == null) return;

        // 觸發與持續
        bool down = Input.GetKeyDown(focusKey);
        bool held = Input.GetKey(focusKey);

        if (down) StartFocus();

        if (_focusing)
        {
            if (!held)
            {
                _timer += Time.unscaledDeltaTime;
                if (_timer >= focusHoldTime) StopFocus();
            }

            // 聚焦中：更新 LookAt 與 Y 軸高度
            UpdateLookAtAndAxes();
        }

        UpdateMarker();
    }

    void StartFocus()
    {
        _focusing = true;
        _timer = 0f;

        // 設定 LookAt：用輔助點更順，否則直接用 target
        if (useLookAtHelper && _helper != null)
        {
            freeLook.LookAt = _helper;
            UpdateHelperPosition(); // 先放到正確位置
        }
        else
        {
            freeLook.LookAt = target;
        }

        // 開啟並加速 recenter（讓相機自己轉向）
        if (enableHeadingRecentering)
        {
            freeLook.m_RecenterToTargetHeading.m_enabled = true;
            freeLook.m_RecenterToTargetHeading.m_WaitTime = headingRecenteringWait;
            freeLook.m_RecenterToTargetHeading.m_RecenteringTime = headingRecenteringTime;
            // 讓「目標朝向」有參考：FreeLook 會以 LookAt 為基準回中
            // 如果你的 BindingMode 不是「WorldSpace」，可考慮改成 WorldSpace 以得到更可預期的方位。
        }

        if (enableYAxisRecentering)
        {
            freeLook.m_YAxisRecentering.m_enabled = true;
            freeLook.m_YAxisRecentering.m_WaitTime = yRecenteringWait;
            freeLook.m_YAxisRecentering.m_RecenteringTime = yRecenteringTime;

            // 直接把 Y 軸拉到我們想要的高度，之後 recenter 會維持住
            freeLook.m_YAxis.Value = Mathf.Clamp01(focusYHeight);
        }

        if (markerUI) markerUI.gameObject.SetActive(true);
    }

    void StopFocus()
    {
        _focusing = false;

        // 還原 recenter 設定（你原本已經有）
        freeLook.m_RecenterToTargetHeading.m_enabled = _origHeadEnable;
        freeLook.m_RecenterToTargetHeading.m_RecenteringTime = _origHeadTime;
        freeLook.m_RecenterToTargetHeading.m_WaitTime = _origHeadWait;

        freeLook.m_YAxisRecentering.m_enabled = _origYEnable;
        freeLook.m_YAxisRecentering.m_RecenteringTime = _origYTime;
        freeLook.m_YAxisRecentering.m_WaitTime = _origYWait;

        // 🔁 還原 LookAt / Follow
        if (restoreLookAtOnStop) freeLook.LookAt = _origLookAt;
        //if (restoreFollowOnStop) freeLook.Follow = _origFollow;

        if (markerUI) markerUI.gameObject.SetActive(false);
    }

    /*void UpdateLookAtAndAxes()
    {
        if (useLookAtHelper && _helper != null) UpdateHelperPosition();
        // 若想再更「積極」對準，亦可在此微調 m_XAxis.Value（手動加減角度）；
        // 但多數情況讓 recenter + LookAt 自然回中會比較順眼。
    }*/
    
    void UpdateLookAtAndAxes()
    {
        if (useLookAtHelper && _helper != null) UpdateHelperPosition();

        if (smoothTurn) DriveFreeLookToTargetYaw();   // ✅ 平滑轉向
        if (smoothY)    DriveFreeLookY();             // ⬆️ 可選的Y軸微調
    }
    
    void DriveFreeLookToTargetYaw()
    {
        // 取玩家→目標的「水平」方向
        Vector3 flat = Vector3.ProjectOnPlane(target.position - player.position, Vector3.up);
        if (flat.sqrMagnitude < 0.0001f) return;

        // 目標方位角（世界空間）
        float desiredYaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;

        // 當前 FreeLook 的 X 軸角度
        float currYaw = freeLook.m_XAxis.Value;

        // SmoothDampAngle 讓角度平滑靠近（不抖、不穿越180°）
        float nextYaw = Mathf.SmoothDampAngle(
            currYaw, desiredYaw, ref _xVel,
            turnEase,                      // 平滑時間
            turnSpeedDegPerSec,            // 角速度上限
            Time.unscaledDeltaTime
        );

        freeLook.m_XAxis.Value = nextYaw;
    }

    void DriveFreeLookY()
    {
        float curr = freeLook.m_YAxis.Value;
        float next = Mathf.SmoothDamp(
            curr, Mathf.Clamp01(desiredYOnFocus),
            ref _yVel, ySmoothTime, Mathf.Infinity, Time.unscaledDeltaTime
        );
        freeLook.m_YAxis.Value = Mathf.Clamp01(next);
    }



    void UpdateHelperPosition()
    {
        // 計算玩家→目標的水平向量
        Vector3 toTarget = target.position - player.position;
        Vector3 flat = Vector3.ProjectOnPlane(toTarget, Vector3.up).normalized;
        if (flat.sqrMagnitude < 0.0001f) flat = player.forward;

        Vector3 pos = player.position + flat * helperDistance;
        float h = Mathf.Lerp(player.position.y, target.position.y, Mathf.Clamp01(helperTargetBlend));
        pos.y = h + helperHeight;

        _helper.position = pos;
    }

    void UpdateMarker()
    {
        if (markerUI == null || target == null || gameCamera == null) return;

        Vector3 world = target.position + markerWorldOffset;
        Vector3 sp = gameCamera.WorldToScreenPoint(world);

        bool isBehind = sp.z < 0f;
        if (isBehind) sp *= -1f;

        Vector2 screen = new Vector2(Screen.width, Screen.height);
        Vector2 pos = sp;
        bool onScreen = sp.z > 0f && sp.x > 0 && sp.x < screen.x && sp.y > 0 && sp.y < screen.y;

        if (onScreen)
        {
            markerUI.position = pos;
            if (rotateOffscreenArrow) markerUI.localEulerAngles = Vector3.zero;
        }
        else
        {
            Vector2 center = screen * 0.5f;
            Vector2 dir = ((Vector2)pos - center).normalized;

            Vector2 edgePos = center + dir * 9999f;
            edgePos.x = Mathf.Clamp(edgePos.x, screenEdgePadding, screen.x - screenEdgePadding);
            edgePos.y = Mathf.Clamp(edgePos.y, screenEdgePadding, screen.y - screenEdgePadding);

            markerUI.position = edgePos;

            if (rotateOffscreenArrow)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                markerUI.localEulerAngles = new Vector3(0, 0, angle - 90f); // 圖示預設朝上
            }
        }

        // 只在聚焦時顯示
        if (!_focusing && markerUI.gameObject.activeSelf) markerUI.gameObject.SetActive(false);
        else if (_focusing && !markerUI.gameObject.activeSelf) markerUI.gameObject.SetActive(true);
    }
}
