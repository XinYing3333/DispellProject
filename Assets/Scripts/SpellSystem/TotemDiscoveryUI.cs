using UnityEngine;
using DG.Tweening;
using System;

namespace DefaultNamespace
{
    [RequireComponent(typeof(CanvasGroup))]
    public class TotemDiscoveryUI : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 顯示 UI 序列：淡入 -> 停留 -> 淡出
        /// </summary>
        /// <param name="onCompleteCallback">全部動畫結束後的回調</param>
        public void Show(Action onCompleteCallback)
        {
            gameObject.SetActive(true);
            _canvasGroup.DOKill();
            _canvasGroup.alpha = 0;
            
            // 暫停遊戲時間
            Time.timeScale = 0f;

            Sequence s = DOTween.Sequence();
            
            // 使用 SetUpdate(true) 確保在 TimeScale = 0 時仍能運行動畫
            s.Append(_canvasGroup.DOFade(1f, 0.5f).SetUpdate(true))
                .AppendInterval(2f)
                .Append(_canvasGroup.DOFade(0f, 0.5f).SetUpdate(true))
                .OnComplete(() => 
                {
                    Time.timeScale = 1f;
                    gameObject.SetActive(false);
                    onCompleteCallback?.Invoke();
                });
            
            s.SetUpdate(true);
        }
    }
}