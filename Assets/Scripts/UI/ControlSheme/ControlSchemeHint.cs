using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using DefaultNamespace.EventBus;
using DefaultNamespace.EventBus.Events.UI;
using UI.Localization;

public class ControlSchemeHint : MonoBehaviour
{
    public enum UIInputMode { KeyboardMouse, Gamepad }

    public static ControlSchemeHint Instance { get; private set; }

    public UIInputMode CurrentMode { get; private set; } = UIInputMode.KeyboardMouse;
    public bool IsGamepad => CurrentMode == UIInputMode.Gamepad;

    public event Action<UIInputMode> OnModeChanged;

    [Header("UI Refs")]
    [SerializeField] private RectTransform toastRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text label;

    [Header("Animation")]
    [SerializeField] private float showSeconds = 1.5f;
    [SerializeField] private float fadeTime = 0.25f;
    [SerializeField] private float slideOffset = 60f;
    [SerializeField] private float punchScale = 1.15f;
    [SerializeField] private float punchDuration = 0.25f;

    [Header("Sprites")]
    [SerializeField] private Sprite controller;
    [SerializeField] private Sprite keyboardMouse;

    [Header("Cursor (optional)")]
    [SerializeField] private bool manageCursor = true;

    private Tween _tween;
    private string _lastScheme;
    private EventBinding<LanguageChanged> _langBinding;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (canvasGroup == null) canvasGroup = toastRoot.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        toastRoot.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _langBinding = new EventBinding<LanguageChanged>(OnLanguageChanged);
        EventBus<LanguageChanged>.Register(_langBinding);
    }

    private void OnDisable()
    {
        EventBus<LanguageChanged>.Deregister(_langBinding);
    }

    private void OnLanguageChanged(LanguageChanged evt)
    {
        // 若當前 Toast 正在顯示，即時更新文字
        if (toastRoot.gameObject.activeInHierarchy)
        {
            RefreshToastText();
        }
    }

    public void OnControlSchemeChanged(PlayerInput pi)
    {
        if (pi == null) return;
        string scheme = pi.currentControlScheme;
        UpdateToastAndMode(scheme);
    }

    private void UpdateToastAndMode(string scheme)
    {
        if (string.IsNullOrEmpty(scheme) || scheme == _lastScheme) return;
        _lastScheme = scheme;

        var newMode = scheme.Contains("Gamepad") ? UIInputMode.Gamepad : UIInputMode.KeyboardMouse;
    
        ApplyCursorState(newMode);

        if (newMode != CurrentMode)
        {
            CurrentMode = newMode;
            // 假設你有這個類別處理政策
            // UICursorPolicy.Instance?.Apply();
            OnModeChanged?.Invoke(CurrentMode);
        }

        RefreshToastText();
        PlayToastAnimation();
    }

    private void RefreshToastText()
    {
        var lang = LocalizationService.Instance != null 
            ? LocalizationService.Instance.CurrentAppLanguage 
            : Language.en;

        if (CurrentMode == UIInputMode.Gamepad)
        {
            iconImage.sprite = controller;
            label.text = lang == Language.zh ? "已切換至 控制器" : "Gamepad Connected";
        }
        else
        {
            iconImage.sprite = keyboardMouse;
            label.text = lang == Language.zh ? "已切換至 鍵盤滑鼠" : "Keyboard & Mouse Connected";
        }
    }

    private void PlayToastAnimation()
    {
        _tween?.Kill();
        _tween = null;

        toastRoot.gameObject.SetActive(true);
        canvasGroup.alpha = 0f;

        Vector3 startPos = toastRoot.anchoredPosition;
        toastRoot.anchoredPosition = startPos - new Vector3(0, slideOffset, 0);
        toastRoot.localScale = Vector3.one * 0.9f;

        _tween = DOTween.Sequence()
            .Append(canvasGroup.DOFade(1f, fadeTime))
            .Join(toastRoot.DOAnchorPosY(startPos.y, fadeTime).SetEase(Ease.OutCubic))
            .Append(toastRoot.DOPunchScale(Vector3.one * (punchScale - 1f), punchDuration, vibrato: 1))
            .AppendInterval(showSeconds)
            .Append(canvasGroup.DOFade(0f, fadeTime))
            .Join(toastRoot.DOAnchorPosY(startPos.y - slideOffset, fadeTime).SetEase(Ease.InCubic))
            .OnComplete(() =>
            {
                toastRoot.gameObject.SetActive(false);
                toastRoot.anchoredPosition = startPos;
            })
            .SetUpdate(true);
    }
    
    private void ApplyCursorState(UIInputMode mode)
    {
        if (!manageCursor) return;

        if (mode == UIInputMode.Gamepad)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}