// HeartsUI_DOTween.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

using EventBus.Events.Health;
using Player; // 🔽 引入 Player 命名空間以讀取 PlayerInputHandler

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
    
    // 🔽 新增：閒置顯示設定
    [Tooltip("滿血時，閒置多久後自動顯示面板")]
    public float idleShowDelay = 3.0f;
    
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

    // 🔽 新增：閒置狀態追蹤
    private float _idleTimer = 0f;
    private bool _isShownByIdle = false;

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

                if (IsFullHp())
                    TryAutoHide();
            }

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

    // 🔽 新增：Update 負責監控玩家移動與閒置邏輯
    private void Update()
    {
        // 如果沒有滿血，或者是沒有抓到 PlayerInputHandler，就不執行閒置顯示邏輯
        if (!IsFullHp() || PlayerInputHandler.Instance == null)
        {
            _idleTimer = 0f;
            _isShownByIdle = false;
            return;
        }

        // 判斷玩家是否正在移動 (根據你的 InputHandler 邏輯)
        bool isMoving = PlayerInputHandler.Instance.MoveInput.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            _idleTimer = 0f; // 重置閒置計時器

            // 如果目前面板是因為閒置才顯示的，玩家一移動就收起來
            if (_isShownByIdle)
            {
                _isShownByIdle = false;
                
                // 確認目前沒有因為剛補血/開場而在執行 AutoHide (避免搶動畫)
                if (_autoHideCo == null) 
                {
                    SlideOut();
                }
            }
        }
        else
        {
            // 玩家沒有移動，開始計算閒置時間
            _idleTimer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            // 當閒置時間達標，且面板還沒被標記為「閒置顯示」時
            if (_idleTimer >= idleShowDelay && !_isShownByIdle)
            {
                _isShownByIdle = true;
                
                // 確保不會被原本受傷/補血的自動隱藏蓋過去
                CancelAutoHide(); 
                SlideIn();
            }
        }
    }

    private void OnHealthChanged(OnHealthChanged e)
    {
        if (!target || e.target != target.gameObject) return;

        Refresh(e.current, e.max);

        int heartsNow = GetHearts(e.current, e.max);
        bool hpDecreased = (_lastHearts >= 0 && heartsNow < _lastHearts);
        _lastHearts = heartsNow;

        // 只要有變化就顯示 (重置閒置狀態，讓變化邏輯優先)
        _isShownByIdle = false;
        SlideIn();

        TryLowHpBeat();

        if (IsFullHp())
            TryAutoHide();
        else
            CancelAutoHide();   

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
        var parent = rt.parent as RectTransform;
        Vector2 shown = rt.anchoredPosition;
        Vector2 size = rt.rect.size;
        Vector2 parentSize = parent ? parent.rect.size : size * 2f;

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
        bool wasLowBeat = IsLowHp();
        if (wasLowBeat) KillBeat();

        heartImage.rectTransform.DOKill();
        heartImage.rectTransform.localScale = Vector3.one;
        Sequence s = DOTween.Sequence().SetUpdate(useUnscaledTime);
        s.Append(heartImage.rectTransform.DOScale(hitBounceScale, hitBounceDuration * 0.6f).SetEase(hitBounceEase));
        s.Append(heartImage.rectTransform.DOScale(1f, hitBounceDuration * 0.4f).SetEase(Ease.OutQuad));
        s.Play();

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