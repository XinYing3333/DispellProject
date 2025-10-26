using System;
using System.Collections;
using DefaultNamespace.EventBus.Events.Core;
using DG.Tweening;
using UnityEngine;

namespace Player.RespawnAndCheckPoint
{
    public class CheckpointUI : MonoBehaviour
    {
        public enum SlideFrom
        {
            Top,
            Bottom,
            Left,
            Right
        }

        [Header("Show/Hide (Slide)")] public SlideFrom slideFrom = SlideFrom.Right;
        [Tooltip("滑入動畫時間（秒）")] public float enterDuration = 0.35f;
        [Tooltip("滑出動畫時間（秒）")] public float exitDuration = 0.3f;
        [Tooltip("非低血時，顯示後多久自動滑出")] public float autoHideDelay = 1.6f;
        [Tooltip("超出可視區的額外距離（像素）")] public float offscreenPadding = 60f;
        public Ease enterEase = Ease.OutCubic;
        public Ease exitEase = Ease.InCubic;

        [Tooltip("使用不受TimeScale影響的更新（建議UI用true）")]
        public bool useUnscaledTime = true;
        
        private RectTransform _rt;
        private Vector2 _shownPos; // 進場後的錨點座標
        private Vector2 _hiddenPos; // 場外座標

        // Tweens
        private Tween _slideTween;
        private Tween _beatTween;
        private Coroutine _autoHideCo;

        // EventBus
        private EventBinding<OnCheckpointUpdated> _binding;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
        }


        private void Update()
        {
            if(Input.GetKey(KeyCode.L))
            {
                SlideIn();
            }
        }

        void OnEnable()
        {
            // 記錄顯示位置（目前錨點）
            _shownPos = _rt.anchoredPosition;
            // 計算場外位置
            _hiddenPos = CalcHiddenPos(_rt, slideFrom, offscreenPadding);

            // 先把它放到場外（避免開場就看到）
            _rt.anchoredPosition = _hiddenPos;

            _binding = new EventBinding<OnCheckpointUpdated>(SlideIn);
            EventBus<OnCheckpointUpdated>.Register(_binding);
        }

        void OnDisable()
        {
            if (_binding != null)
            {
                EventBus<OnCheckpointUpdated>.Deregister(_binding);
                _binding = null;
            }

            KillSlide();
        }

        // ---------- Slide In/Out ----------
        private void SlideIn()
        {
            if (_autoHideCo != null)
            {
                StopCoroutine(_autoHideCo);
                _autoHideCo = null;
            }

            KillSlide();
            _slideTween = _rt.DOAnchorPos(_shownPos, enterDuration)
                .SetEase(enterEase)
                .SetUpdate(useUnscaledTime);
            
            StartCoroutine(Co_AutoHide());
        }

        private void SlideOut()
        {
            KillSlide();
            _slideTween = _rt.DOAnchorPos(_hiddenPos, exitDuration)
                .SetEase(exitEase)
                .SetUpdate(useUnscaledTime);
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
    }
}