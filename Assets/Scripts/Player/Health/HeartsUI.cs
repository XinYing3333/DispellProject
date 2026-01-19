// HeartsUI_DOTween.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

using EventBus.Events.Health;

[RequireComponent(typeof(RectTransform))]
public class HeartsUI : MonoBehaviour
{
    public enum SlideFrom { Top, Bottom, Left, Right }

    [Header("Target")]
    public Health target;

    [Header("Single Image & Stages")]
    public Image heartImage;
    [Tooltip("索引=剩餘顆心數（0,1,2,3,4...）。放滿對應數量的sprite")]
    public List<Sprite> heartStages = new();

    [Header("Show/Hide (Slide)")]
    public SlideFrom slideFrom = SlideFrom.Top;
    [Tooltip("滑入動畫時間（秒）")]
    public float enterDuration = 0.35f;
    [Tooltip("滑出動畫時間（秒）")]
    public float exitDuration = 0.3f;
    [Tooltip("非低血時，顯示後多久自動滑出")]
    public float autoHideDelay = 1.6f;
    [Tooltip("超出可視區的額外距離（像素）")]
    public float offscreenPadding = 60f;
    public Ease enterEase = Ease.OutCubic;
    public Ease exitEase  = Ease.InCubic;
    [Tooltip("使用不受TimeScale影響的更新（建議UI用true）")]
    public bool useUnscaledTime = true;

    [Header("Low HP")]
    [Tooltip("當心數 ≤ 此值時常駐 + 心跳")]
    public int lowHpThreshold = 2;
    [Tooltip("低血心跳的放大倍率")]
    public float lowBeatScale = 1.08f;
    [Tooltip("低血心跳速度（單次往返時間）")]
    public float lowBeatPeriod = 0.6f;

    [Header("On Damage (Hit Bounce)")]
    [Tooltip("受傷時的瞬間彈跳倍率")]
    public float hitBounceScale = 1.18f;
    [Tooltip("受傷彈跳時間（硬直短促）")]
    public float hitBounceDuration = 0.18f;
    public Ease hitBounceEase = Ease.OutBack;

    [Header("Init")]
    public bool showOnEnable = true;

    private RectTransform _rt;
    private Vector2 _shownPos;      // 進場後的錨點座標
    private Vector2 _hiddenPos;     // 場外座標
    private int _lastHearts = -1;

    // Tweens
    private Tween _slideTween;
    private Tween _beatTween;
    private Coroutine _autoHideCo;

    // EventBus
    private EventBinding<OnHealthChanged> _binding;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (!heartImage) heartImage = GetComponentInChildren<Image>(true);
    }

    private void OnEnable()
    {
        _rt = GetComponent<RectTransform>();
        _shownPos = _rt.anchoredPosition;
        _hiddenPos = CalcHiddenPos(_rt, slideFrom, offscreenPadding);
        _rt.anchoredPosition = _hiddenPos;

        _binding = new EventBinding<OnHealthChanged>(OnHealthChanged);
        EventBus<OnHealthChanged>.Register(_binding);

        if (target)
        {
            Refresh(target.GetCurrent(), target.GetMax());
            _lastHearts = GetHearts(target.GetCurrent(), target.GetMax());

            if (showOnEnable)
            {
                SlideIn();

                // 🔽 開場時：如果一開始就是滿血就排程隱藏，否則常駐
                if (IsFullHp())
                    TryAutoHide();
            }

            // 低血心跳照舊
            TryLowHpBeat();
        }
    }


    void OnDisable()
    {
        if (_binding != null)
        {
            EventBus<OnHealthChanged>.Deregister(_binding);
            _binding = null;
        }

        KillSlide();
        KillBeat();

        if (_autoHideCo != null)
        {
            StopCoroutine(_autoHideCo);
            _autoHideCo = null;
        }
    }

    private void OnHealthChanged(OnHealthChanged e)
    {
        if (!target || e.target != target.gameObject) return;

        Refresh(e.current, e.max);

        int heartsNow = GetHearts(e.current, e.max);
        bool hpDecreased = (_lastHearts >= 0 && heartsNow < _lastHearts);
        _lastHearts = heartsNow;

        // 只要有變化就顯示
        SlideIn();

        // 低血就跳
        TryLowHpBeat();

        // 🔽 這裡改掉：只有「滿血」才會自動收回去
        if (IsFullHp())
            TryAutoHide();
        else
            CancelAutoHide();   // 不滿就不要收

        if (hpDecreased)
            PlayHitBounce();
    }


    private void Refresh(int current, int max)
    {
        if (!target || heartImage == null) return;
        int hearts = GetHearts(current, max);

        if (heartStages != null && heartStages.Count > 0)
        {
            int idx = Mathf.Clamp(hearts, 0, heartStages.Count - 1);
            if (heartStages[idx] != null)
                heartImage.sprite = heartStages[idx];
        }
    }

    private int GetHearts(int current, int max)
    {
        int size = Mathf.Max(1, target.heartSize);
        return Mathf.CeilToInt(current / (float)size);
    }

    // ---------- Slide In/Out ----------
    private void SlideIn()
    {
        if (_autoHideCo != null) { StopCoroutine(_autoHideCo); _autoHideCo = null; }
        KillSlide();
        _slideTween = _rt.DOAnchorPos(_shownPos, enterDuration)
            .SetEase(enterEase)
            .SetUpdate(useUnscaledTime);
    }

    private void SlideOut()
    {
        KillSlide();
        _slideTween = _rt.DOAnchorPos(_hiddenPos, exitDuration)
            .SetEase(exitEase)
            .SetUpdate(useUnscaledTime);
    }

    private void TryAutoHide()
    {
        if (_autoHideCo != null) { StopCoroutine(_autoHideCo); _autoHideCo = null; }
        _autoHideCo = StartCoroutine(Co_AutoHide());
    }

    private IEnumerator Co_AutoHide()
    {
        float t = 0f;
        while (t < autoHideDelay)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
        SlideOut();
        _autoHideCo = null;
    }

    private void KillSlide()
    {
        if (_slideTween != null && _slideTween.IsActive())
            _slideTween.Kill();
        _slideTween = null;
    }

    private static Vector2 CalcHiddenPos(RectTransform rt, SlideFrom from, float padding)
    {
        // 以父Rect做可視區，將元件推到外面
        var parent = rt.parent as RectTransform;
        Vector2 shown = rt.anchoredPosition;
        Vector2 size = rt.rect.size;
        Vector2 parentSize = parent ? parent.rect.size : size * 2f;

        // 以錨點相對推離。這裡採用保守做法：直接朝方向推到螢幕外 + padding
        switch (from)
        {
            case SlideFrom.Top:
                return new Vector2(shown.x, shown.y + parentSize.y * 0.5f + size.y * 0.5f + padding);
            case SlideFrom.Bottom:
                return new Vector2(shown.x, shown.y - parentSize.y * 0.5f - size.y * 0.5f - padding);
            case SlideFrom.Left:
                return new Vector2(shown.x - parentSize.x * 0.5f - size.x * 0.5f - padding, shown.y);
            case SlideFrom.Right:
                return new Vector2(shown.x + parentSize.x * 0.5f + size.x * 0.5f + padding, shown.y);
        }
        return shown;
    }

    // ---------- Hit Bounce ----------
    private void PlayHitBounce()
    {
        // 終止低血心跳以免互搶縮放，播放後再恢復（若仍低血）
        bool wasLowBeat = IsLowHp();
        if (wasLowBeat) KillBeat();

        // 從1縮到1.0再回到1.0（用Sequence做一個小彈）
        heartImage.rectTransform.DOKill();
        heartImage.rectTransform.localScale = Vector3.one;
        Sequence s = DOTween.Sequence().SetUpdate(useUnscaledTime);
        s.Append(heartImage.rectTransform.DOScale(hitBounceScale, hitBounceDuration * 0.6f).SetEase(hitBounceEase));
        s.Append(heartImage.rectTransform.DOScale(1f, hitBounceDuration * 0.4f).SetEase(Ease.OutQuad));
        s.Play();

        // 播完若仍低血，恢復心跳
        if (wasLowBeat && IsLowHp()) StartLowHpBeat();
    }

    // ---------- Low HP Beat ----------
    private bool IsLowHp()
    {
        return _lastHearts >= 0 && _lastHearts <= lowHpThreshold;
    }

    private void TryLowHpBeat()
    {
        if (IsLowHp()) StartLowHpBeat();
        else            KillBeat();
    }

    private void StartLowHpBeat()
    {
        if (_beatTween != null && _beatTween.IsActive()) return;

        var t = heartImage.rectTransform;
        t.DOKill();
        t.localScale = Vector3.one;

        // 心跳：1 -> 放大 -> 1 -> 放大 … 無限
        _beatTween = t.DOScale(lowBeatScale, lowBeatPeriod * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(useUnscaledTime);
    }

    private void KillBeat()
    {
        if (_beatTween != null && _beatTween.IsActive())
            _beatTween.Kill();
        _beatTween = null;

        if (heartImage) heartImage.rectTransform.localScale = Vector3.one;
    }
    
    private bool IsFullHp()
    {
        if (!target) return false;
        int currentHearts = GetHearts(target.GetCurrent(), target.GetMax());
        int maxHearts     = GetHearts(target.GetMax(), target.GetMax());
        return currentHearts >= maxHearts;
    }

    private void CancelAutoHide()
    {
        if (_autoHideCo != null)
        {
            StopCoroutine(_autoHideCo);
            _autoHideCo = null;
        }
    }

}