using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

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
    
    // UI 排版快取
    private RectTransform _centerRect, _prevRect, _nextRect;
    private float _origCenterX, _origPrevX, _origNextX;
    private int _lastIndex = -1;

    private void Awake()
    {
        foreach (var entry in spellDataList)
        {
            if (!_uiLookup.ContainsKey(entry.type))
                _uiLookup.Add(entry.type, entry);
        }

        // 快取初始座標，作為動畫的絕對歸位點
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
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnSpellChanged -= UpdateUI;
        }
    }

    private void UpdateUI(SpellType currentType)
    {
        List<SpellType> spells = inventory.GetUnlockedSpells();
        int currentIndex = spells.IndexOf(currentType);

        if (spells.Count == 0) return;

        // 1. 計算滾動方向 (判斷是向左切還是向右切，處理陣列循環)
        int direction = 0;
        if (_lastIndex != -1 && spells.Count > 1)
        {
            if (currentIndex == 0 && _lastIndex == spells.Count - 1) direction = 1;
            else if (currentIndex == spells.Count - 1 && _lastIndex == 0) direction = -1;
            else direction = (currentIndex > _lastIndex) ? 1 : -1;
        }
        _lastIndex = currentIndex;

        // 2. 計算起始偏移量
        // 如果切換下一個(direction=1)，所有物件需從「右側」往原位滑動
        float slideDistance = Mathf.Abs(_origNextX - _origCenterX);
        float startOffsetX = (direction == 1) ? slideDistance : (direction == -1 ? -slideDistance : 0f);

        int prevIdx = (currentIndex - 1 + spells.Count) % spells.Count;
        int nextIdx = (currentIndex + 1) % spells.Count;

        // 3. 執行替換與動畫
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

        if (_uiLookup.ContainsKey(currentType))
        {
            spellNameText.text = _uiLookup[currentType].spellName;
            
            spellNameText.DOKill();
            spellNameText.color = new Color(spellNameText.color.r, spellNameText.color.g, spellNameText.color.b, 0f);
            spellNameText.DOFade(1f, tweenDuration);
        }

        if (showDebug) Debug.Log($"[SpellUI] 畫面已更新為: {currentType} | 方向: {direction}");
    }

    private void ExecuteSlotAnimation(Image img, RectTransform rect, SpellType type, bool isActive, float origX, float startOffsetX)
    {
        if (_uiLookup.ContainsKey(type))
        {
            img.sprite = _uiLookup[type].icon;

            img.DOKill();
            rect.DOKill();

            // 若有偏移，表示發生切換，強制重置錨點 X 至反方向
            if (startOffsetX != 0f)
            {
                rect.anchoredPosition = new Vector2(origX + startOffsetX, rect.anchoredPosition.y);
            }

            // 動態目標數值
            Color targetColor = isActive ? activeColor : inactiveColor;
            Vector3 targetScale = isActive ? Vector3.one * centerScale : Vector3.one * sideScale;

            // 觸發並行補間：位移歸位 + 縮放 + 顏色變化
            rect.DOAnchorPosX(origX, tweenDuration).SetEase(tweenEase);
            rect.DOScale(targetScale, tweenDuration).SetEase(tweenEase);
            
            // 處理淡入：如果物件從邊緣進入，將初始透明度設為 0，強化流動感
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