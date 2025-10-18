using System.Collections;
using UnityEngine;

public class FadeToTransparentEffect : TransitionEffect
{
    [Header("References")]
    [Tooltip("一張全黑 Image 物件上的 CanvasGroup，alpha 預設為 1（全黑）")]
    public CanvasGroup blackCanvas;

    [Header("Timings")]
    [Range(0.05f, 3f)] public float fadeDuration = 0.3f;

    public override IEnumerator Play()
    {
        if (!blackCanvas) yield break;

        blackCanvas.gameObject.SetActive(true);
        blackCanvas.alpha = 1f; // 起始為全黑

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            // 👉 逐漸從 1 → 0
            blackCanvas.alpha = Mathf.Clamp01(1f - (t / fadeDuration));
            yield return null;
        }

        blackCanvas.alpha = 0f;
        blackCanvas.gameObject.SetActive(false); // 淡出完可關閉物件
    }
}