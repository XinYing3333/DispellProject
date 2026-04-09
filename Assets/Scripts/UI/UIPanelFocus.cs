using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIPanelFocus : MonoBehaviour
{
    [Header("Focus / Selection")]
    public GameObject firstSelected;
    public bool selectOnEnable = true;
    public bool guardSelectionOnGamepad = true;

    [Tooltip("當遺失焦點時，偵測到移動輸入是否自動恢復選取")]
    public bool autoRecoverOnInput = true;

    [Header("Cursor Policy")]
    public bool requireCursorOnOpen = true;

    [Header("Time Control")]
    public bool pauseTimeOnOpen = false;
    public bool resumeOnClose = true;

    private UIFocusGuard _guard;
    private bool _cursorRegistered;
    private bool _pausedByThis;

    private void Awake()
    {
        _guard = FindAnyObjectByType<UIFocusGuard>();
    }

    private void OnEnable()
    {
        if (requireCursorOnOpen)
        {
            UICursorPolicy.Instance?.PanelOpened(this);
            _cursorRegistered = true;
        }

        if (!firstSelected)
            firstSelected = GetComponentInChildren<Selectable>(true)?.gameObject;

        if (guardSelectionOnGamepad)
            _guard?.PushFirstSelected(firstSelected);

        if (selectOnEnable)
            StartCoroutine(SelectNextFrame());

        if (pauseTimeOnOpen && Time.timeScale > 0f)
        {
            Time.timeScale = 0f;
            _pausedByThis = true;
        }
    }

    private void Update()
    {
        // 處理 Escape 關閉
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            gameObject.SetActive(false);
            return;
        }

        // 核心邏輯：偵測焦點遺失與移動輸入
        if (autoRecoverOnInput)
        {
            HandleFocusRecovery();
        }
    }

    private void HandleFocusRecovery()
    {
        var es = EventSystem.current;
        if (es == null || firstSelected == null) return;

        // 當目前沒有任何物件被選取時
        if (es.currentSelectedGameObject == null)
        {
            // 偵測導航輸入 (支援 Controller 與 Keyboard)
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
            {
                es.SetSelectedGameObject(firstSelected);
            }
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
        if (_cursorRegistered)
        {
            UICursorPolicy.Instance?.PanelClosed(this);
            _cursorRegistered = false;
        }

        if (guardSelectionOnGamepad)
            _guard?.PopFirstSelected(firstSelected);

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