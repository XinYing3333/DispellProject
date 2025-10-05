using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MenuExpandController1 : MonoBehaviour
{
    public GameObject expandedPanel;            // 展開的外層面板
    public GameObject contentGroup;             // 展開面板內部的實際內容（要等展開完成才顯示）
    public LayoutElement layoutElement;         // 控制高度的 LayoutElement

    public float collapsedHeight = 30f;         // 原始高度
    public float expandedHeight = 150f;         // 展開後高度
    public float expandDuration = 0.3f;         // 動畫秒數

    private bool isExpanded = false;
    private Coroutine currentCoroutine;

    void Start()
    {
        layoutElement.preferredHeight = collapsedHeight;
        expandedPanel.SetActive(false);
        if (contentGroup != null)
            contentGroup.SetActive(false); // 初始隱藏內容
    }

    public void ToggleExpand()
    {
        isExpanded = !isExpanded;

        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        if (isExpanded)
        {
            expandedPanel.SetActive(true); // 要先啟用展開面板
            if (contentGroup != null)
                contentGroup.SetActive(false); // 動畫前先隱藏內容
            currentCoroutine = StartCoroutine(ExpandRoutine());
        }
        else
        {
            currentCoroutine = StartCoroutine(CollapseRoutine());
        }
    }

    private IEnumerator ExpandRoutine()
    {
        yield return StartCoroutine(AnimateHeight(collapsedHeight, expandedHeight));
        if (contentGroup != null)
            contentGroup.SetActive(true); // 動畫後顯示內容
    }

    private IEnumerator CollapseRoutine()
    {
        if (contentGroup != null)
            contentGroup.SetActive(false); // 動畫前先隱藏內容
        yield return StartCoroutine(AnimateHeight(expandedHeight, collapsedHeight));
        expandedPanel.SetActive(false); // 高度收合後關掉整個面板
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
}
