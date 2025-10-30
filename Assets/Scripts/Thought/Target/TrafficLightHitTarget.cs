using System.Collections;
using DefaultNamespace.Thought;
using Player.InteractionSystem;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using DG.Tweening;

public class TrafficLightHitTarget : MonoBehaviour, IHitReceiver
{
    [Header("Refs")]
    public RoadFader road;              // 斑馬線淡入/淡出控制
    public Collider crossRoad;          // 阻擋用碰撞器（有就用；沒有就忽略）

    [Header("Timing")]
    public float fadeInTime  = 1f;
    public float openSeconds = 6f;      // 倒數持續時間
    public float fadeOutTime = 1f;

    [Header("OnceEvent")]
    public UnityEvent onFirstHit;

    private bool _consumed;

    [Header("Options")]
    public bool oneAtATime = true;      // 防止重入
    private bool _busy;

    // ====== UI 部分 ======
    [Header("UI")]
    public TextMeshProUGUI countdownText;       // 顯示倒數的 TMP
    public RectTransform countdownPulseTarget;  // 要跳動的 UI（整個小面板）

    [Tooltip("正常顏色")]
    public Color normalColor = Color.white;

    [Tooltip("最後3秒顏色")]
    public Color dangerColor = Color.red;

    [Tooltip("低於這個秒數開始變紅")]
    public int dangerThreshold = 3;

    [Header("UI Tween")]
    public float pulseAmount = 0.15f;
    public float pulseDuration = 0.22f;
    public int pulseVibrato = 6;
    public float pulseElasticity = 0.6f;

    [Header("Show / Hide Tween")]
    public float showDuration = 0.25f;
    public float hideDuration = 0.25f;

    private Vector3 _uiOrigScale;
    private Tweener _showHideTween;

    private void Awake()
    {
        if (countdownPulseTarget)
        {
            _uiOrigScale = countdownPulseTarget.localScale;
            // 一開始先藏起來
            countdownPulseTarget.localScale = Vector3.zero;
        }
    }

    public void OnHit(ThoughtPayloadSO payload)
    {
        if (oneAtATime && _busy) return;
        StartCoroutine(RunCycle());
    }

    IEnumerator RunCycle()
    {
        _busy = true;

        // 1) 漸入顯示路
        if (road) yield return road.FadeIn(fadeInTime);

        if (!_consumed) NotifyHit();

        // 2) 開路
        if (crossRoad) crossRoad.enabled = true;

        // 3) 倒數 + UI
        yield return StartCoroutine(Co_CountdownUI(Mathf.CeilToInt(openSeconds)));

        // 4) 路漸隱
        if (road) yield return road.FadeOut(fadeOutTime);

        // 5) 關路
        if (crossRoad) crossRoad.enabled = false;

        _busy = false;
    }

    // 倒數協程：開始時顯示 UI，結束時隱藏 UI
    private IEnumerator Co_CountdownUI(int seconds)
    {
        // 🟢 顯示 UI（OutBack）
        ShowCountdownUI();

        for (int i = seconds; i > 0; i--)
        {
            // 更新文字
            if (countdownText)
            {
                countdownText.text = i.ToString();

                // 顏色控制
                countdownText.color = (i <= dangerThreshold) ? dangerColor : normalColor;
            }

            // 每秒跳動
            PlayCountdownPulse();

            yield return new WaitForSeconds(1f);
        }

        // 數完要清空或留 0，都可以
        if (countdownText)
            countdownText.text = "";

        // 🔴 隱藏 UI（InBack）
        HideCountdownUI();

        // 給隱藏動畫一點點時間跑（可選，不想等可以拿掉）
        yield return new WaitForSeconds(hideDuration);
    }

    private void ShowCountdownUI()
    {
        if (!countdownPulseTarget) return;

        // 停掉前一個
        if (_showHideTween != null && _showHideTween.IsActive())
            _showHideTween.Kill();

        countdownPulseTarget.localScale = Vector3.zero;
        _showHideTween = countdownPulseTarget
            .DOScale(_uiOrigScale, showDuration)
            .SetEase(Ease.OutBack);
    }

    private void HideCountdownUI()
    {
        if (!countdownPulseTarget) return;

        if (_showHideTween != null && _showHideTween.IsActive())
            _showHideTween.Kill();

        _showHideTween = countdownPulseTarget
            .DOScale(0f, hideDuration)
            .SetEase(Ease.InBack);
    }

    private void PlayCountdownPulse()
    {
        if (!countdownPulseTarget) return;

        // 這裡我們是「在原本大小上再做一次 punch」
        // 所以要保證先回到原本大小
        countdownPulseTarget.localScale = _uiOrigScale;
        countdownPulseTarget.DOPunchScale(Vector3.one * pulseAmount, pulseDuration, pulseVibrato, pulseElasticity);
    }

    private void NotifyHit()
    {
        if (_consumed) return;
        _consumed = true;
        onFirstHit?.Invoke();
    }
}
