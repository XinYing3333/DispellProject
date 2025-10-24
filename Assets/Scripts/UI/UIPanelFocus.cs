using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIPanelFocus : MonoBehaviour
{
    [Header("Focus / Selection")]
    [Tooltip("面板打開時預設選取的物件（Button/Toggle/Slider...）")]
    public GameObject firstSelected;

    [Tooltip("面板開啟時是否自動選取 firstSelected")]
    public bool selectOnEnable = true;

    [Tooltip("在搖桿模式下啟用『選取保護』，確保永遠有選中物件")]
    public bool guardSelectionOnGamepad = true;

    [Header("Cursor Policy")]
    [Tooltip("面板打開時，於鍵鼠模式下要求顯示游標")]
    public bool requireCursorOnOpen = true;

    [Header("Time Control")]
    [Tooltip("打開面板時是否暫停時間（Time.timeScale = 0）")]
    public bool pauseTimeOnOpen = false;

    [Tooltip("如果要暫停時間，關閉面板時是否恢復時間")]
    public bool resumeOnClose = true;

    private UIFocusGuard _guard;
    private bool _cursorRegistered;
    private bool _pausedByThis;   // 確保只恢復自己暫停的情況

    private void Awake()
    {
        _guard = FindAnyObjectByType<UIFocusGuard>();
    }

    private void OnEnable()
    {
        // 1️⃣ 游標策略（鍵鼠模式開面板→顯示游標；搖桿模式不影響）
        if (requireCursorOnOpen)
        {
            UICursorPolicy.Instance?.PanelOpened(this);
            _cursorRegistered = true;
        }

        // 2️⃣ 搖桿選取保護堆疊 + 設定 firstSelected
        if (!firstSelected)
            firstSelected = GetComponentInChildren<Selectable>(true)?.gameObject;

        if (guardSelectionOnGamepad)
            _guard?.PushFirstSelected(firstSelected);

        // 3️⃣ 下一幀再 Select
        if (selectOnEnable)
            StartCoroutine(SelectNextFrame());

        // 4️⃣ 暫停時間
        if (pauseTimeOnOpen && Time.timeScale > 0f)
        {
            Time.timeScale = 0f;
            _pausedByThis = true;
        }
    }

    private void OnDisable()
    {
        Cleanup();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        // 恢復游標策略
        if (_cursorRegistered)
        {
            UICursorPolicy.Instance?.PanelClosed(this);
            _cursorRegistered = false;
        }

        // Pop 搖桿選取堆疊
        if (guardSelectionOnGamepad)
            _guard?.PopFirstSelected(firstSelected);

        // 恢復時間
        if (resumeOnClose && _pausedByThis)
        {
            Time.timeScale = 1f;
            _pausedByThis = false;
        }
    }

    private IEnumerator SelectNextFrame()
    {
        yield return null;
        var es = EventSystem.current;
        if (es && firstSelected)
        {
            es.SetSelectedGameObject(null);
            es.SetSelectedGameObject(firstSelected);
        }
    }
}
