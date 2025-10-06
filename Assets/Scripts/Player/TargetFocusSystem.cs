using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // 若用舊 Input，移除這行並改成 KeyCode 判斷
using Cinemachine;
using UnityEngine.UI;

// TODO：加入inputsystem
public class TargetFocusSystem : MonoBehaviour
{
    [Header("Refs")]
    public CinemachineFreeLook freeLook;   // FreeLook 相機
    public Transform player;               // 玩家
    public Transform target;               // 聚焦目標
    public Camera gameCamera;              // 主相機
    public RectTransform markerUI;         // UI 圖示 (Image 的 RectTransform)

    [Header("Focus Trigger")]
    public KeyCode focusKey = KeyCode.T;   // 聚焦按鍵
    public float focusHoldTime = 1.2f;     // 點按持續秒數（長按則持續）

    [Header("Smooth Turn")]
    public bool smoothTurn = true;
    [Tooltip("最大平滑旋轉速度(度/秒)")]
    public float turnSpeedDegPerSec = 360f;
    [Tooltip("越小越柔順；0.15~0.30 常用")]
    public float turnEase = 0.18f;
    private float _xVel;

    [Header("Smooth Y (可選)")]
    public bool smoothY = true;
    [Range(0f, 1f)] public float desiredYOnFocus = 0.45f; // 聚焦時 FreeLook 的 Y 軸值
    public float ySmoothTime = 0.25f;
    private float _yVel;

    [Header("Marker UI")]
    public Vector3 markerWorldOffset = new Vector3(0, 1.5f, 0);
    [Range(8f, 48f)] public float screenEdgePadding = 24f;
    public bool rotateOffscreenArrow = true;

    [Header("Debug")]
    public bool forceWorldBinding = true;   // 啟動時強制設成 WorldSpace/WorldForward
    public bool logAngles = false;

    // 狀態
    private bool _focusing;
    private float _timer;

    // 備份 recenter（如果你平時有用）
    private bool _origHeadEnable;
    private float _origHeadTime, _origHeadWait;
    private bool _origYEnable;
    private float _origYTime, _origYWait;

    void Awake()
    {
        if (!gameCamera) gameCamera = Camera.main;
        if (markerUI) markerUI.gameObject.SetActive(false);

        if (freeLook != null)
        {
            // 備份 recenter 設定
            _origHeadEnable = freeLook.m_RecenterToTargetHeading.m_enabled;
            _origHeadTime   = freeLook.m_RecenterToTargetHeading.m_RecenteringTime;
            _origHeadWait   = freeLook.m_RecenterToTargetHeading.m_WaitTime;

            _origYEnable    = freeLook.m_YAxisRecentering.m_enabled;
            _origYTime      = freeLook.m_YAxisRecentering.m_RecenteringTime;
            _origYWait      = freeLook.m_YAxisRecentering.m_WaitTime;

            // ✅ 強制把三個 Rig 都設為「WorldSpace / WorldForward」
            if (forceWorldBinding)
            {
                for (int i = 0; i < 3; i++)
                {
                    var rig = freeLook.GetRig(i);
                    if (!rig) continue;

                    var body = rig.GetCinemachineComponent<CinemachineOrbitalTransposer>();
                    if (body != null)
                    {
                        body.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
                        // Heading 定義為世界前方，X 軸值就能直接用世界角度控制
                        body.m_Heading.m_Definition = CinemachineOrbitalTransposer.Heading.HeadingDefinition.WorldForward;
                    }

                    var aim = rig.GetCinemachineComponent<CinemachineComposer>();
                    if (aim != null)
                    {
                        // 輕微阻尼讓轉向更柔
                        aim.m_HorizontalDamping = Mathf.Max(aim.m_HorizontalDamping, 0.2f);
                        aim.m_VerticalDamping   = Mathf.Max(aim.m_VerticalDamping,   0.2f);
                    }
                }

                // 也建議在 FreeLook 上加 CinemachineCollider 擴充件（於 Inspector）
                // 以避免穿牆：Avoid Obstacles=On, Pull Camera Forward=On, Damping=0.3~0.5
            }
        }
    }

    void OnDisable()
    {
        if (_focusing) StopFocus();

        // 還原 recenter（如果你平常有用）
        if (freeLook != null)
        {
            freeLook.m_RecenterToTargetHeading.m_enabled = _origHeadEnable;
            freeLook.m_RecenterToTargetHeading.m_RecenteringTime = _origHeadTime;
            freeLook.m_RecenterToTargetHeading.m_WaitTime = _origHeadWait;

            freeLook.m_YAxisRecentering.m_enabled = _origYEnable;
            freeLook.m_YAxisRecentering.m_RecenteringTime = _origYTime;
            freeLook.m_YAxisRecentering.m_WaitTime = _origYWait;
        }
    }

    void Update()
    {
        if (freeLook == null || player == null || target == null) return;

        bool down = Input.GetKeyDown(focusKey);
        bool held = Input.GetKey(focusKey);

        if (down) StartFocus();

        if (_focusing)
        {
            if (!held)
            {
                _timer += Time.unscaledDeltaTime;
                if (_timer >= focusHoldTime)
                    StopFocus();
            }

            UpdateFocusRotation();
        }

        UpdateMarker();
    }

    void StartFocus()
    {
        _focusing = true;
        _timer = 0f;

        // 聚焦期間：明確關閉 recenter，避免它把相機拉去別的角度
        freeLook.m_RecenterToTargetHeading.m_enabled = false;
        freeLook.m_YAxisRecentering.m_enabled = false;

        if (markerUI) markerUI.gameObject.SetActive(true);
    }

    void StopFocus()
    {
        _focusing = false;
        if (markerUI) markerUI.gameObject.SetActive(false);

        // 退出聚焦後是否要還原 recenter：這裡先不還原，交由 OnDisable 或你自己的流程決定
        // 若你想立即還原，也可以解開下列註解：
        /*
        freeLook.m_RecenterToTargetHeading.m_enabled = _origHeadEnable;
        freeLook.m_YAxisRecentering.m_enabled = _origYEnable;
        */
    }

    // ✅ 平滑旋轉 FreeLook X / Y 軸
    void UpdateFocusRotation()
    {
        if (smoothTurn) DriveFreeLookToTargetYaw();
        if (smoothY)    DriveFreeLookY();
    }

    // 只讓相機繞玩家旋轉，面向「玩家->目標」的方向（不動 LookAt/Follow）
    void DriveFreeLookToTargetYaw()
    {
        // 玩家 → 目標 的水平向量
        Vector3 flat = Vector3.ProjectOnPlane(target.position - player.position, Vector3.up);
        if (flat.sqrMagnitude < 0.0001f) return;

        // 用「世界前方」作基準算出期望方位角（-180~180）
        float worldYaw = Vector3.SignedAngle(Vector3.forward, flat.normalized, Vector3.up);

        // 因為我們的 LookAt 是「玩家」，希望相機位在玩家的「反方向」軌道上，鏡頭就會看向目標
        float desiredYaw = Mathf.DeltaAngle(0f, worldYaw + 180f); // 加 180 度取得相機應在的軌道角

        // 當前 FreeLook 的 X 軸值（角度）
        float currYaw = freeLook.m_XAxis.Value;

        // 平滑靠近
        float nextYaw = Mathf.SmoothDampAngle(
            currYaw, desiredYaw, ref _xVel,
            turnEase, turnSpeedDegPerSec, Time.unscaledDeltaTime
        );

        freeLook.m_XAxis.Value = nextYaw;

        if (logAngles)
            Debug.Log($"[Focus] worldYaw={worldYaw:F1}, desiredYaw={desiredYaw:F1}, currYaw={currYaw:F1} -> next={nextYaw:F1}");
    }

    // 平滑調整 FreeLook 的仰角 (Y Axis)
    void DriveFreeLookY()
    {
        float curr = freeLook.m_YAxis.Value;
        float next = Mathf.SmoothDamp(
            curr, Mathf.Clamp01(desiredYOnFocus),
            ref _yVel, ySmoothTime, Mathf.Infinity, Time.unscaledDeltaTime
        );
        freeLook.m_YAxis.Value = Mathf.Clamp01(next);
    }

    // 顯示目標指示 UI（在螢幕上或邊緣）
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
                markerUI.localEulerAngles = new Vector3(0, 0, angle - 90f);
            }
        }

        if (!_focusing && markerUI.gameObject.activeSelf) markerUI.gameObject.SetActive(false);
        else if (_focusing && !markerUI.gameObject.activeSelf) markerUI.gameObject.SetActive(true);
    }
}