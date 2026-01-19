using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ThoughtCounterUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private RectTransform rootRect;
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private RectTransform pulseTarget;

    [Header("Config")]
    [SerializeField] private int maxThought = 20;
    [SerializeField] private CollectionSystem.CollectedType listenType = CollectionSystem.CollectedType.Though;

    [Header("Tween Settings")]
    [SerializeField] private float punchAmount = 0.15f;
    [SerializeField] private float punchDuration = 0.25f;
    [SerializeField] private int vibrato = 8;
    [SerializeField] private float elasticity = 0.6f;

    [Header("Enter Animation")]
    [SerializeField] private float enterOffsetY = 200f;
    [SerializeField] private float enterDuration = 0.6f;
    [SerializeField] private Ease enterEase = Ease.OutCubic;

    private Vector3 _origScale;
    private Vector2 _origAnchoredPos;
    private Tweener _pulseTween;

    private void Awake()
    {
        if (!rootRect) rootRect = GetComponent<RectTransform>();
        if (!pulseTarget) pulseTarget = rootRect;
        if (!slider) slider = GetComponentInChildren<Slider>();
        if (!countText) countText = GetComponentInChildren<TextMeshProUGUI>();

        _origScale = pulseTarget.localScale;
        _origAnchoredPos = rootRect.anchoredPosition;

        slider.minValue = 0;
        slider.maxValue = maxThought;
        UpdateUI(CollectionSystem.GetItemCount(listenType));

        // 先放到畫面上方 + 隱藏
        rootRect.anchoredPosition = _origAnchoredPos + new Vector2(0, enterOffsetY);
    }

    private void OnEnable()
    {
        CollectionSystem.OnCollected += OnCollected;
        PlayEnterAnimation();
    }

    private void OnDisable()
    {
        CollectionSystem.OnCollected -= OnCollected;
    }

    private void OnCollected(CollectionSystem.CollectedType type, int total)
    {
        if (type != listenType) return;
        UpdateUI(total);
        PlayPulse();
    }

    private void UpdateUI(int total)
    {
        int clamped = Mathf.Clamp(total, 0, maxThought);
        slider.DOValue(clamped, 0.25f).SetEase(Ease.OutQuad);
        if (countText)
            countText.text = $"{clamped}/{maxThought}";
    }

    private void PlayPulse()
    {
        if (_pulseTween != null && _pulseTween.IsActive())
            _pulseTween.Kill();

        pulseTarget.localScale = _origScale;
        _pulseTween = pulseTarget.DOPunchScale(Vector3.one * punchAmount, punchDuration, vibrato, elasticity);
    }

    private void PlayEnterAnimation()
    {
        Vector2 endPos = _origAnchoredPos;
        Vector2 startPos = _origAnchoredPos + new Vector2(0, enterOffsetY);

        rootRect.anchoredPosition = startPos;
        rootRect.DOAnchorPos(endPos, enterDuration)
            .SetEase(enterEase);
    }
}
