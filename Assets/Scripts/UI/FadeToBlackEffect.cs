// FadeToBlackEffect.cs
using System.Collections;
using UnityEngine;

public class FadeToBlackEffect : TransitionEffect
{
    [Header("References")]
    [Tooltip("一張全黑 Image 物件上的 CanvasGroup，alpha 預設 0")]
    public CanvasGroup blackCanvas;

    [Header("Timings")]
    [Range(0.05f, 3f)] public float fadeDuration = 0.8f;

    public override IEnumerator Play()
    {
        if (!blackCanvas) yield break;

        blackCanvas.gameObject.SetActive(true);
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            blackCanvas.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        blackCanvas.alpha = 1f;
    }
}