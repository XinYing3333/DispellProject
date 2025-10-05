using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MenuExpandController : MonoBehaviour
{
    public GameObject expandedPanel;            // 展開的外層面板
    public CanvasGroup contentGroup;            // 控制淡入淡出的 CanvasGroup
    public LayoutElement layoutElement;         // 控制高度的 LayoutElement

    public float collapsedHeight = 30f;         // 原始高度
    public float expandedHeight = 150f;         // 展開後高度
    public float expandDuration = 0.3f;         // 高度動畫時間
    public float fadeDuration = 0.2f;           // 淡入淡出動畫時間

    private bool isExpanded = false;
    private Coroutine currentCoroutine;

    void Start()
    {
        layoutElement.preferredHeight = collapsedHeight;
        expandedPanel.SetActive(false);

        if (contentGroup != null)
        {
            contentGroup.alpha = 0f;
            contentGroup.interactable = false;
            contentGroup.blocksRaycasts = false;
        }
    }

    public void ToggleExpand()
    {
        isExpanded = !isExpanded;

        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        if (isExpanded)
        {
            expandedPanel.SetActive(true);
            currentCoroutine = StartCoroutine(ExpandRoutine());
        }
        else
        {
            currentCoroutine = StartCoroutine(CollapseRoutine());
        }
    }

    private IEnumerator ExpandRoutine()
    {
        // 高度展開動畫
        yield return StartCoroutine(AnimateHeight(collapsedHeight, expandedHeight));

        // 淡入內容
        if (contentGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(contentGroup, 0f, 1f, fadeDuration));
            contentGroup.interactable = true;
            contentGroup.blocksRaycasts = true;
        }
    }

    private IEnumerator CollapseRoutine()
    {
        // 淡出內容
        if (contentGroup != null)
        {
            contentGroup.interactable = false;
            contentGroup.blocksRaycasts = false;
            yield return StartCoroutine(FadeCanvasGroup(contentGroup, 1f, 0f, fadeDuration));
        }

        // 高度收合動畫
        yield return StartCoroutine(AnimateHeight(expandedHeight, collapsedHeight));
        expandedPanel.SetActive(false);
    }

    private IEnumerator AnimateHeight(float from, float to)
    {
        float time = 0f;

        while (time < expandDuration)
        {
            time += Time.deltaTime;
            float t = time / expandDuration;
            layoutElement.preferredHeight = Mathf.Lerp(from, to, t);
            yield return null;
        }

        layoutElement.preferredHeight = to;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float time = 0f;
        cg.alpha = from;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            cg.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        cg.alpha = to;
    }
}
