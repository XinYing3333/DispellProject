using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace.ControlSheme;
using DefaultNamespace.Tutorial;
using Player; 
using DG.Tweening;
using EventBus.Events.Tutorial;


[RequireComponent(typeof(CanvasGroup))]
public class TutorialUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RectTransform iconContainer;
    [SerializeField] private GameObject iconPrefab;
    [SerializeField] private GameObject completionCheckmark; 
    [SerializeField] private InputBindingLibrary bindingLibrary;

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float completeDelay = 0.8f; 
    [SerializeField] private Vector2 hiddenPosition = new Vector2(500, 0);
    [SerializeField] private Vector2 visiblePosition = Vector2.zero;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private TutorialData currentData;
    private Coroutine displayCoroutine;

    // 狀態追蹤
    private HashSet<TutorialRequirementType> _metRequirements = new HashSet<TutorialRequirementType>(); 
    private bool _isStepCompleted = false;
    
    private EventBinding<OnTutorialRequirementMet> _binding;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        
        canvasGroup.alpha = 0;
        rectTransform.anchoredPosition = hiddenPosition;
        if (completionCheckmark) completionCheckmark.SetActive(false);
    }

    private void OnEnable()
    {
        // 1. 訂閱控制方案切換 (用於響應式圖示)
        if (ControlSchemeHint.Instance != null)
            ControlSchemeHint.Instance.OnModeChanged += RefreshIcons;

        // 2. 訂閱 EventBus 外部邏輯事件
        _binding = new EventBinding<OnTutorialRequirementMet>(OnExternalRequirementMet);
        EventBus<OnTutorialRequirementMet>.Register(_binding);    }

    private void OnDisable()
    {
        if (ControlSchemeHint.Instance != null)
            ControlSchemeHint.Instance.OnModeChanged -= RefreshIcons;
        
        if (_binding == null) return; 
        EventBus<OnTutorialRequirementMet>.Deregister(_binding);
        _binding = null; 
    }

    public void SetupAndShow(TutorialData data)
    {
        if (displayCoroutine != null) StopCoroutine(displayCoroutine);

        // 初始化狀態
        currentData = data;
        _isStepCompleted = false;
        _metRequirements.Clear();
        if (completionCheckmark) completionCheckmark.SetActive(false);

        // 填充內容
        titleText.text = data.actionName;
        descText.text = data.description;
        
        if (data.tutorialVideo != null)
        {
            videoPlayer.clip = data.tutorialVideo;
            videoPlayer.Prepare();
        }

        // 初始化圖示
        var mode = ControlSchemeHint.Instance != null 
            ? ControlSchemeHint.Instance.CurrentMode 
            : ControlSchemeHint.UIInputMode.KeyboardMouse;
        RefreshIcons(mode);

        displayCoroutine = StartCoroutine(DisplaySequence());
    }

    private void Update()
    {
        if (currentData == null || _isStepCompleted) return;

        bool changed = false;
        foreach (var req in currentData.requiredRequirements)
        {
            if (_metRequirements.Contains(req)) continue;

            if (PlayerInputHandler.Instance.CheckActionPressed(req) || 
                PlayerInputHandler.Instance.CheckPlayerState(req))
            {
                if (_metRequirements.Add(req)) changed = true;
            }
        }
        if (changed) CheckAllRequirements();
    }

    /// <summary>
    /// 處理來自 EventBus 的外部事件（如：成功吸入、擊敗敵人）
    /// </summary>
    private void OnExternalRequirementMet(OnTutorialRequirementMet e)
    {
        if (currentData == null || _isStepCompleted) return;

        if (currentData.requiredRequirements.Contains(e.Requirement))
        {
            if (_metRequirements.Add(e.Requirement)) CheckAllRequirements();
        }
    }

    private void CheckAllRequirements()
    {
        // 只有當已達成的需求數量等於或超過清單總量時才完成
        if (_metRequirements.Count >= currentData.requiredRequirements.Count)
        {
            SetComplete();
        }
    }

    private void SetComplete()
    {
        _isStepCompleted = true;
        AudioManager.Instance.PlaySFX(SFXType.Complete);
        if (completionCheckmark)
        {
            completionCheckmark.SetActive(true);
            completionCheckmark.transform.localScale = Vector3.zero;
            completionCheckmark.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
            completionCheckmark.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
        }
    }

    private void RefreshIcons(ControlSchemeHint.UIInputMode mode)
    {
        if (currentData == null) return;

        // 清除舊圖示
        foreach (Transform child in iconContainer) Destroy(child.gameObject);

        foreach (ActionName action in currentData.requiredInputActions)
        {
            // 實例化抽離後的 Prefab
            GameObject iconObj = Instantiate(iconPrefab, iconContainer);
        
            // 取得組件並初始化
            if (iconObj.TryGetComponent<InputIconDisplay>(out var display))
            {
                display.SetAction(action);
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(iconContainer);
    }

    private IEnumerator DisplaySequence()
    {
        while (!videoPlayer.isPrepared) yield return null;

        // 進入動畫
        videoPlayer.Play();
        rectTransform.DOAnchorPos(visiblePosition, fadeDuration).SetEase(Ease.OutCubic);
        canvasGroup.DOFade(1f, fadeDuration);

        // 等待玩家達成所有條件
        while (!_isStepCompleted)
        {
            yield return null;
        }

        // 完成後的視覺停頓
        yield return new WaitForSeconds(completeDelay);

        // 退出動畫
        rectTransform.DOAnchorPos(hiddenPosition, fadeDuration).SetEase(Ease.InCubic);
        canvasGroup.DOFade(0f, fadeDuration);

        yield return new WaitForSeconds(fadeDuration);
        
        videoPlayer.Stop();
        currentData = null;
        TutorialManager.Instance.OnTutorialComplete();
    }
}