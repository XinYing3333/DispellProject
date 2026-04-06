using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasGroup))]
public class TutorialUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private RectTransform iconContainer;
    [SerializeField] private GameObject iconPrefab;
    [SerializeField] private InputBindingLibrary bindingLibrary; // 必須指派資料庫

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float displayDuration = 6.0f;
    [SerializeField] private Vector2 hiddenPosition = new Vector2(500, 0);
    [SerializeField] private Vector2 visiblePosition = Vector2.zero;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Coroutine displayCoroutine;
    private TutorialData currentData; // 追蹤當前資料以利模式切換

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup.alpha = 0;
        rectTransform.anchoredPosition = hiddenPosition;
    }

    private void OnEnable()
    {
        // 註冊模式切換事件
        if (ControlSchemeHint.Instance != null)
            ControlSchemeHint.Instance.OnModeChanged += RefreshIcons;
    }

    private void OnDisable()
    {
        // 註銷事件防止記憶體洩漏
        if (ControlSchemeHint.Instance != null)
            ControlSchemeHint.Instance.OnModeChanged -= RefreshIcons;
    }

    public void SetupAndShow(TutorialData data)
    {
        if (displayCoroutine != null) StopCoroutine(displayCoroutine);
        
        currentData = data; // 儲存當前教學資料
        titleText.text = data.actionName;
        descText.text = data.description;
        
        if (data.tutorialVideo != null)
        {
            videoPlayer.clip = data.tutorialVideo;
            videoPlayer.Prepare();
        }

        // 初始化時根據當前模式生成圖示
        var currentMode = ControlSchemeHint.Instance != null 
            ? ControlSchemeHint.Instance.CurrentMode 
            : ControlSchemeHint.UIInputMode.KeyboardMouse;
            
        RefreshIcons(currentMode);

        displayCoroutine = StartCoroutine(DisplaySequence());
    }

    // 核心修改：統一圖示生成邏輯
    private void RefreshIcons(ControlSchemeHint.UIInputMode mode)
    {
        if (currentData == null) return;

        // 1. 清除現有圖示
        foreach (Transform child in iconContainer) Destroy(child.gameObject);

        // 2. 根據模式判定 (Gamepad or KeyboardMouse)
        bool isGamepad = (mode == ControlSchemeHint.UIInputMode.Gamepad);

        // 3. 從 bindingLibrary 動態抓取對應 Action 的圖示
        if (currentData.requiredInputActions != null)
        {
            foreach (string actionName in currentData.requiredInputActions)
            {
                if (string.IsNullOrEmpty(actionName)) continue;
                
                GameObject iconObj = Instantiate(iconPrefab, iconContainer);
                Sprite s = bindingLibrary.GetSprite(actionName, isGamepad);
                
                if (s != null)
                {
                    iconObj.GetComponent<Image>().sprite = s;
                }
            }
        }

        // 4. 立即刷新 Layout 以應用 Pivot X=1 的向左延伸效果
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(iconContainer);
    }

    private IEnumerator DisplaySequence()
    {
        while (!videoPlayer.isPrepared) yield return null;
        
        float elapsed = 0;
        videoPlayer.Play();
        
        // 淡入與位移
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / fadeDuration);
            canvasGroup.alpha = t;
            rectTransform.anchoredPosition = Vector2.Lerp(hiddenPosition, visiblePosition, t);
            yield return null;
        }

        // 停留
        yield return new WaitForSeconds(displayDuration);

        // 淡出與位移
        elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / fadeDuration);
            canvasGroup.alpha = 1 - t;
            rectTransform.anchoredPosition = Vector2.Lerp(visiblePosition, hiddenPosition, t);
            yield return null;
        }

        videoPlayer.Stop();
        currentData = null; // 清除狀態
        TutorialManager.Instance.OnTutorialComplete();
    }
}