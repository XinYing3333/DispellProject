using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PanelFadeController : MonoBehaviour
{
    public CanvasGroup[] panels; // 對應 Sound / Graphic / Tutor
    public float fadeDuration = 0.3f;

    private int currentIndex = 0;
    private bool isFading = false;

    void Start()
    {
        // 初始化：只顯示第一個，其他隱藏
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].alpha = (i == 0 ? 1 : 0);
            panels[i].interactable = (i == 0);
            panels[i].blocksRaycasts = (i == 0);
        }
    }

    public void SwitchToPanel(int index)
    {
        if (index == currentIndex || isFading)
            return;

        StartCoroutine(FadePanel(index));
    }

    private IEnumerator FadePanel(int newIndex)
    {
        isFading = true;

        CanvasGroup oldPanel = panels[currentIndex];
        CanvasGroup newPanel = panels[newIndex];

        // 確保新 Panel 可見，但透明
        newPanel.gameObject.SetActive(true);
        newPanel.alpha = 0;
        newPanel.interactable = false;
        newPanel.blocksRaycasts = false;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;

            oldPanel.alpha = 1 - t;
            newPanel.alpha = t;

            yield return null;
        }

        // 完成淡入淡出
        oldPanel.alpha = 0;
        oldPanel.interactable = false;
        oldPanel.blocksRaycasts = false;

        newPanel.alpha = 1;
        newPanel.interactable = true;
        newPanel.blocksRaycasts = true;

        currentIndex = newIndex;
        isFading = false;
    }
}
