using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class ForMenuResetGame : MonoBehaviour
    {
        [Header("UI 設定")]
        [Tooltip("掛載 CanvasGroup 的重置提示視窗")]
        public CanvasGroup feedbackCanvasGroup;
        
        [Tooltip("淡入淡出的持續時間")]
        public float fadeDuration = 0.3f;
        
        [Tooltip("視窗停留時間")]
        public float displayTime = 1.0f;

        private bool _isResetting = false;

        public void ResetGame()
        {
            if (_isResetting) return;
            StartCoroutine(ResetSequence());
        }

        private IEnumerator ResetSequence()
        {
            _isResetting = true;

            // 1. 執行重置邏輯
            PlayerPrefs.DeleteAll();
            CollectionSystem.ClearCollection();
            Time.timeScale = 1;

            // 2. 淡入提示 (Alpha: 0 -> 1)
            if (feedbackCanvasGroup != null)
            {
                yield return StartCoroutine(FadeCanvasGroup(0, 1));
                
                // 3. 停留
                yield return new WaitForSecondsRealtime(displayTime);

                // 4. 淡出 (Alpha: 1 -> 0)
                yield return StartCoroutine(FadeCanvasGroup(1, 0));
            }

            // 5. 重新載入場景
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private IEnumerator FadeCanvasGroup(float start, float end)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime; // 使用 unscaled 以防 Time.timeScale 為 0
                feedbackCanvasGroup.alpha = Mathf.Lerp(start, end, elapsed / fadeDuration);
                yield return null;
            }
            feedbackCanvasGroup.alpha = end;
        }
    }
}