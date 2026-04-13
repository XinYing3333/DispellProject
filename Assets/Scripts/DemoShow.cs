using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DemoShow : MonoBehaviour
{
    [Header("Focus Settings")]
    public GameObject firstButton;
    public bool autoRecoverOnInput = true;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;
    
    private CanvasGroup _canvasGroup;
    private GameObject _lastSelected;
    private bool _isVisible = false;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        
        // 初始狀態：隱藏且不阻擋點擊
        _canvasGroup.alpha = 0;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 外部呼叫介面：顯示結束面板並開始淡入
    /// </summary>
    public void ShowDemoEndPanel()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(0, 1, true));
    }

    private void Update()
    {
        if (!_isVisible) return;

        HandleFocusLock();
    }

    private void HandleFocusLock()
    {
        var es = EventSystem.current;
        if (es == null || firstButton == null) return;

        // 1. 焦點遺失處理
        if (es.currentSelectedGameObject == null)
        {
            float input = Mathf.Abs(Input.GetAxisRaw("Horizontal")) + Mathf.Abs(Input.GetAxisRaw("Vertical"));
            if (input > 0.1f || autoRecoverOnInput)
            {
                es.SetSelectedGameObject(_lastSelected ?? firstButton);
            }
        }
        // 2. 焦點跑出面板處理（防止選到背景物件）
        else if (!es.currentSelectedGameObject.transform.IsChildOf(this.transform))
        {
            es.SetSelectedGameObject(firstButton);
        }
        else
        {
            _lastSelected = es.currentSelectedGameObject;
        }
    }

    private IEnumerator FadeRoutine(float start, float end, bool interactive)
    {
        float elapsed = 0f;
        
        // 如果是顯示，先強制選取
        if (interactive)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstButton);
            _lastSelected = firstButton;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(start, end, elapsed / fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = end;
        _canvasGroup.interactable = interactive;
        _canvasGroup.blocksRaycasts = interactive;
        _isVisible = interactive;
    }

    // 按鈕事件：回到選單 (範例)
    public void OnClickReturnMenu()
    {
        PlayerPrefs.DeleteAll();
        Time.timeScale = 1;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    // 按鈕事件：離開遊戲
    public void OnClickQuit()
    {
        PlayerPrefs.DeleteAll();
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}