using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace.ControlSheme;
using DefaultNamespace.EventBus.Events.UI;
using DefaultNamespace.Tutorial;
using Player;
using DG.Tweening;
using EventBus.Events.Tutorial;
using UI.Localization;


[RequireComponent(typeof(CanvasGroup))]
public class TutorialUI : MonoBehaviour
{
    [Header("UI Components")] [SerializeField]
    private TextMeshProUGUI titleText;

    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RectTransform iconContainer;
    [SerializeField] private GameObject iconPrefab;
    [SerializeField] private GameObject completionCheckmark;
    [SerializeField] private InputBindingLibrary bindingLibrary;

    [Header("Animation Settings")] [SerializeField]
    private float fadeDuration = 0.4f;

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
    private EventBinding<LanguageChanged> _langBinding;

    private Vector3 _originalCheckmarkScale;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        canvasGroup.alpha = 0;
        rectTransform.anchoredPosition = hiddenPosition;

        // --- 修改部分：初始化時記錄原始縮放並隱藏 ---
        if (completionCheckmark)
        {
            // 記錄你在 Inspector 設定的原始大小
            _originalCheckmarkScale = completionCheckmark.transform.localScale;
            // 確保一開始是關閉的
            completionCheckmark.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // 1. 訂閱控制方案切換 (用於響應式圖示)
        if (ControlSchemeHint.Instance != null)
            ControlSchemeHint.Instance.OnModeChanged += RefreshIcons;

        // 2. 訂閱 EventBus 外部邏輯事件
        _binding = new EventBinding<OnTutorialRequirementMet>(OnExternalRequirementMet);
        EventBus<OnTutorialRequirementMet>.Register(_binding);
        _langBinding = new EventBinding<LanguageChanged>(OnLanguageChanged);
        EventBus<LanguageChanged>.Register(_langBinding);
    }

    private void OnDisable()
    {
        if (ControlSchemeHint.Instance != null)
            ControlSchemeHint.Instance.OnModeChanged -= RefreshIcons;

        EventBus<OnTutorialRequirementMet>.Deregister(_binding);
        EventBus<LanguageChanged>.Deregister(_langBinding); // 解除註冊
    }

    public void SetupAndShow(TutorialData data)
    {
        if (displayCoroutine != null) StopCoroutine(displayCoroutine);

        // 1. 立即初始化狀態，這必須在任何異步邏輯（如影片準備）之前
        currentData = data;
        _isStepCompleted = false;
        _metRequirements.Clear();

        // 重置 Checkmark
        if (completionCheckmark)
        {
            completionCheckmark.transform.DOKill();
            completionCheckmark.SetActive(false);
            completionCheckmark.transform.localScale = _originalCheckmarkScale;
        }

        // 2. 重要：在 UI 顯示前，先跑一次「即時狀態檢查」
        // 防止玩家在 UI 出現前就已經達成了某些持續性條件（如：站在某個區域、按住某個鍵）
        PreCheckImmediateRequirements();

        RefreshText();

        // 處理影片與圖示
        if (data.tutorialVideo != null)
        {
            videoPlayer.clip = data.tutorialVideo;
            videoPlayer.Prepare();
        }

        var mode = ControlSchemeHint.Instance != null
            ? ControlSchemeHint.Instance.CurrentMode
            : ControlSchemeHint.UIInputMode.KeyboardMouse;
        RefreshIcons(mode);

        // 啟動顯示協程
        displayCoroutine = StartCoroutine(DisplaySequence());
    }

    private void PreCheckImmediateRequirements()
    {
        if (currentData == null) return;

        foreach (var req in currentData.requiredRequirements)
        {
            // 檢查 PlayerInputHandler 裡的即時狀態
            // 如果玩家已經在做這件事了，直接標記為達成
            if (PlayerInputHandler.Instance.CheckActionPressed(req) ||
                PlayerInputHandler.Instance.CheckPlayerState(req))
            {
                _metRequirements.Add(req);
            }

            // 如果你有對接 DataManager，這裡也可以檢查永久性狀態
            // 例如：if (DataManager.Instance.gameData.isFirstAdsorbDone && req == TutorialRequirementType.Adsorb)
        }

        // 如果一進來就全達成了，直接進入完成狀態
        CheckAllRequirements();
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

    private void OnLanguageChanged(LanguageChanged e)
    {
        RefreshText();
    }

    // 抽離文字更新邏輯
    private void RefreshText()
    {
        if (currentData == null) return;

        var lang = LocalizationService.Instance != null
            ? LocalizationService.Instance.CurrentAppLanguage
            : Language.en;

        var content = currentData.GetContent(lang);
        titleText.text = content.title;
        descText.text = content.desc;
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
            // 1. 先殺掉該物件上正在跑的所有動畫
            completionCheckmark.transform.DOKill();

            // 2. 確保狀態重置
            completionCheckmark.SetActive(true);
            completionCheckmark.transform.localScale = Vector3.zero;

            // 3. 使用 Sequence 依序執行，確保 Punch 是在 Scale 到 1 之後才震動
            Sequence seq = DOTween.Sequence();
            seq.Append(completionCheckmark.transform.DOScale(_originalCheckmarkScale, 0.2f).SetEase(Ease.OutBack));
            seq.Append(completionCheckmark.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f));
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