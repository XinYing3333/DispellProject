using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

public class ControlSchemeHint : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private RectTransform toastRoot; // 改成RectTransform比較好做位移
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text label;

    [Header("Animation")]
    [SerializeField] private float showSeconds = 1.5f;
    [SerializeField] private float fadeTime = 0.25f;
    [SerializeField] private float slideOffset = 60f;      // 起始下滑距離
    [SerializeField] private float punchScale = 1.15f;     // 彈跳放大倍率
    [SerializeField] private float punchDuration = 0.25f;  // 彈跳時長

    [Header("Sprites")]
    [SerializeField] private Sprite controller;  
    [SerializeField] private Sprite keyboardMouse; 

    
    private Tween _tween;
    private string _lastScheme;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = toastRoot.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        toastRoot.gameObject.SetActive(false);
    }

    // 讓 PlayerInput inspector 直接綁這個
    public void OnControlSchemeChanged(PlayerInput pi)
    {
        if (pi == null) return;
        string scheme = pi.currentControlScheme;
        UpdateToast(scheme);
    }

    private void UpdateToast(string scheme)
    {
        if (string.IsNullOrEmpty(scheme) || scheme == _lastScheme) return;
        _lastScheme = scheme;

        string text;
        if (scheme.Contains("Gamepad"))
        {
            text = "已切換至 控制器";
            iconImage.sprite = controller;
        }
        else
        {
            text = "已切換至 鍵盤滑鼠";
            iconImage.sprite = keyboardMouse;
        }
        label.text = text;

        PlayToastAnimation();
    }

    private void PlayToastAnimation()
    {
        // 清理舊動畫
        _tween?.Kill();
        _tween = null;

        toastRoot.gameObject.SetActive(true);
        canvasGroup.alpha = 0f;

        // 重設初始位置與縮放
        Vector3 startPos = toastRoot.anchoredPosition;
        toastRoot.anchoredPosition = startPos - new Vector3(0, slideOffset, 0);
        toastRoot.localScale = Vector3.one * 0.9f;

        // 淡入 + 上滑
        _tween = DOTween.Sequence()
            .Append(canvasGroup.DOFade(1f, fadeTime))
            .Join(toastRoot.DOAnchorPosY(startPos.y, fadeTime).SetEase(Ease.OutCubic))
            // 彈跳縮放
            .Append(toastRoot.DOPunchScale(Vector3.one * (punchScale - 1f), punchDuration, vibrato: 1))
            // 停留
            .AppendInterval(showSeconds)
            // 淡出 + 下滑
            .Append(canvasGroup.DOFade(0f, fadeTime))
            .Join(toastRoot.DOAnchorPosY(startPos.y - slideOffset, fadeTime).SetEase(Ease.InCubic))
            .OnComplete(() =>
            {
                toastRoot.gameObject.SetActive(false);
                toastRoot.anchoredPosition = startPos;
            })
            .SetUpdate(true); // 時間不受TimeScale影響（暫停畫面時仍顯示）
    }
}
