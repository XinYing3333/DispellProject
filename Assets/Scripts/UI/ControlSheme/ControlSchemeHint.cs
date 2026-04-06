using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

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
    [SerializeField] private bool manageCursor = true; // 勾選則自動顯示/隱藏游標

    private Tween _tween;
    private string _lastScheme;

    private void Awake()
    {
        if (Instance) { Destroy(gameObject); return; }
        Instance = this;

        if (canvasGroup == null) canvasGroup = toastRoot.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        toastRoot.gameObject.SetActive(false);
    }

    // 讓 PlayerInput inspector 直接綁這個
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
        if (newMode != CurrentMode)
        {
            CurrentMode = newMode;

            // ★ 交由 UICursorPolicy 依「是否有面板開著」＋「目前控制方案」決定游標
            UICursorPolicy.Instance?.Apply();

            OnModeChanged?.Invoke(CurrentMode);
        }

        // 原本的 toast 顯示邏輯保留
        if (CurrentMode == UIInputMode.Gamepad)
        {
            label.text = "已切換至 控制器";
            iconImage.sprite = controller;
        }
        else
        {
            label.text = "已切換至 鍵盤滑鼠";
            iconImage.sprite = keyboardMouse;
        }

        PlayToastAnimation();
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
}
