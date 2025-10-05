using UnityEngine;
using TMPro;
using System.Collections;

public class LoadingTextAnimation : MonoBehaviour
{
    public TMP_Text loadingText;
    private string baseText = "Loading";
    private int dotCount = 0;

    void Start()
    {
        StartCoroutine(AnimateLoadingText());
    }

    IEnumerator AnimateLoadingText()
    {
        while (true)
        {
            dotCount = (dotCount + 1) % 4;
            loadingText.text = baseText + new string('.', dotCount);
            yield return new WaitForSeconds(0.5f);
        }
    }
}
