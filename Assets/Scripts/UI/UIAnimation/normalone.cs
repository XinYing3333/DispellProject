using UnityEngine;

public class normalone : MonoBehaviour
{
    public RectTransform targetUI;      // 要縮放的 UI 物件
    public float animationTime = 0.3f;  // 動畫持續時間

    private bool isOpen = false;        // 當前是否開啟
    private Coroutine currentCoroutine;

    void Start()
    {
        if (targetUI != null)
        {
            targetUI.localScale = Vector3.zero; // 初始為關閉狀態
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // 切換開關狀態
            isOpen = !isOpen;

            // 若已經有動畫在跑，先停止
            if (currentCoroutine != null)
                StopCoroutine(currentCoroutine);

            // 開啟則放大，否則縮小
            Vector3 from = targetUI.localScale;
            Vector3 to = isOpen ? Vector3.one : Vector3.zero;
            currentCoroutine = StartCoroutine(ScaleUI(from, to));
        }
    }

    System.Collections.IEnumerator ScaleUI(Vector3 from, Vector3 to)
    {
        float elapsed = 0f;
        while (elapsed < animationTime)
        {
            float t = elapsed / animationTime;
            targetUI.localScale = Vector3.Lerp(from, to, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        targetUI.localScale = to;
    }
}
