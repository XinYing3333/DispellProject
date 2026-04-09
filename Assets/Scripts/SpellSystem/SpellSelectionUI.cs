using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using DefaultNamespace.EventBus;
using DefaultNamespace.EventBus.Events.UI;
using UI.Localization;

public class SpellSelectionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpellInventoryController inventory;

    [Header("UI Elements")]
    [SerializeField] private Image centerIcon;
    [SerializeField] private Image prevIcon;
    [SerializeField] private Image nextIcon;
    [SerializeField] private TextMeshProUGUI spellNameText;

    [Header("Color Settings")]
    [SerializeField] private List<SpellUIEntry> spellDataList;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(1, 1, 1, 0.5f);
    
    [Header("Tween Settings")]
    [SerializeField] private float tweenDuration = 0.25f;
    [SerializeField] private float centerScale = 1.25f;
    [SerializeField] private float sideScale = 0.85f;
    [SerializeField] private Ease tweenEase = Ease.OutBack;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private Dictionary<SpellType, SpellUIEntry> _uiLookup = new Dictionary<SpellType, SpellUIEntry>();
    private EventBinding<LanguageChanged> _langBinding;
    
    private RectTransform _centerRect, _prevRect, _nextRect;
    private float _origCenterX, _origPrevX, _origNextX;
    private int _lastIndex = -1;
    private SpellType _currentTypeCache;

    private void Awake()
    {
        foreach (var entry in spellDataList)
        {
            if (!_uiLookup.ContainsKey(entry.type))
                _uiLookup.Add(entry.type, entry);
        }

        _centerRect = centerIcon.rectTransform;
        _prevRect = prevIcon.rectTransform;
        _nextRect = nextIcon.rectTransform;

        _origCenterX = _centerRect.anchoredPosition.x;
        _origPrevX = _prevRect.anchoredPosition.x;
        _origNextX = _nextRect.anchoredPosition.x;
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            inventory.OnSpellChanged += UpdateUI;
        }

        // 註冊語言切換事件
        _langBinding = new EventBinding<LanguageChanged>(OnLanguageChanged);
        EventBus<LanguageChanged>.Register(_langBinding);
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnSpellChanged -= UpdateUI;
        }

        // 解除語言切換事件
        EventBus<LanguageChanged>.Deregister(_langBinding);
    }

    private void OnLanguageChanged(LanguageChanged evt)
    {
        RefreshText();
    }

    private void UpdateUI(SpellType currentType)
    {
        _currentTypeCache = currentType;
        List<SpellType> spells = inventory.GetUnlockedSpells();
        int currentIndex = spells.IndexOf(currentType);

        if (spells.Count == 0) return;

        int direction = 0;
        if (_lastIndex != -1 && spells.Count > 1)
        {
            if (currentIndex == 0 && _lastIndex == spells.Count - 1) direction = 1;
            else if (currentIndex == spells.Count - 1 && _lastIndex == 0) direction = -1;
            else direction = (currentIndex > _lastIndex) ? 1 : -1;
        }
        _lastIndex = currentIndex;

        float slideDistance = Mathf.Abs(_origNextX - _origCenterX);
        float startOffsetX = (direction == 1) ? slideDistance : (direction == -1 ? -slideDistance : 0f);

        int prevIdx = (currentIndex - 1 + spells.Count) % spells.Count;
        int nextIdx = (currentIndex + 1) % spells.Count;

        ExecuteSlotAnimation(centerIcon, _centerRect, spells[currentIndex], true, _origCenterX, startOffsetX);
        
        if (spells.Count > 1)
        {
            prevIcon.gameObject.SetActive(true);
            nextIcon.gameObject.SetActive(true);
            ExecuteSlotAnimation(prevIcon, _prevRect, spells[prevIdx], false, _origPrevX, startOffsetX);
            ExecuteSlotAnimation(nextIcon, _nextRect, spells[nextIdx], false, _origNextX, startOffsetX);
        }
        else
        {
            prevIcon.gameObject.SetActive(false);
            nextIcon.gameObject.SetActive(false);
        }

        RefreshText();

        if (showDebug) Debug.Log($"[SpellUI] 更新: {currentType} | 方向: {direction}");
    }

    private void RefreshText()
    {
        if (!_uiLookup.ContainsKey(_currentTypeCache)) return;

        var entry = _uiLookup[_currentTypeCache];
        var lang = LocalizationService.Instance != null 
            ? LocalizationService.Instance.CurrentAppLanguage 
            : Language.en;

        spellNameText.text = entry.GetLocalizedName(lang);
        
        // 觸發文字淡入動畫
        spellNameText.DOKill();
        spellNameText.color = new Color(spellNameText.color.r, spellNameText.color.g, spellNameText.color.b, 0f);
        spellNameText.DOFade(1f, tweenDuration);
    }

    private void ExecuteSlotAnimation(Image img, RectTransform rect, SpellType type, bool isActive, float origX, float startOffsetX)
    {
        if (_uiLookup.ContainsKey(type))
        {
            img.sprite = _uiLookup[type].icon;
            img.DOKill();
            rect.DOKill();

            if (startOffsetX != 0f)
            {
                rect.anchoredPosition = new Vector2(origX + startOffsetX, rect.anchoredPosition.y);
            }

            Color targetColor = isActive ? activeColor : inactiveColor;
            Vector3 targetScale = isActive ? Vector3.one * centerScale : Vector3.one * sideScale;

            rect.DOAnchorPosX(origX, tweenDuration).SetEase(tweenEase);
            rect.DOScale(targetScale, tweenDuration).SetEase(tweenEase);
            
            if (startOffsetX != 0f)
            {
                Color startColor = targetColor;
                startColor.a = 0f;
                img.color = startColor;
            }
            img.DOFade(targetColor.a, tweenDuration);
        }
    }
}