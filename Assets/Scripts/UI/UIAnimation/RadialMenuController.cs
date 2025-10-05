using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class RadialMenuController : MonoBehaviour
{
    public GameObject radialMenuGameObject;    // 環狀選單父物件
    public RectTransform[] menuItems;
    public float radius = 150f;

    private int selectedIndex = 0;
    private bool isOpen = false;

    private Coroutine animationCoroutine;

    void Start()
    {
        if (radialMenuGameObject != null)
        {
            radialMenuGameObject.SetActive(false);
            radialMenuGameObject.transform.localScale = Vector3.zero;
        }

        ArrangeItems();
        HighlightSelected();

        for (int i = 0; i < menuItems.Length; i++)
        {
            int index = i;
            Button btn = menuItems[i].GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => OnClickItem(index));
        }
    }

    void Update()
    {
        // 持續清除 UI 選中物件，防止鍵盤失效
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (animationCoroutine != null)
                StopCoroutine(animationCoroutine);

            if (isOpen)
                animationCoroutine = StartCoroutine(CloseMenu());
            else
                animationCoroutine = StartCoroutine(OpenMenu());

            isOpen = !isOpen;
        }

        if (!isOpen) return;

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            selectedIndex = (selectedIndex + 1) % menuItems.Length;
            HighlightSelected();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            selectedIndex = (selectedIndex - 1 + menuItems.Length) % menuItems.Length;
            HighlightSelected();
        }

        if (Input.GetKeyDown(KeyCode.Space))
            Summon(selectedIndex);
    }

    IEnumerator OpenMenu()
    {
        radialMenuGameObject.SetActive(true);

        float duration = 0.2f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float scale = Mathf.Lerp(0f, 1f, timer / duration);
            radialMenuGameObject.transform.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }

        radialMenuGameObject.transform.localScale = Vector3.one;
        HighlightSelected();
    }

    IEnumerator CloseMenu()
    {
        float duration = 0.2f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 0f, timer / duration);
            radialMenuGameObject.transform.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }

        radialMenuGameObject.transform.localScale = Vector3.zero;
        radialMenuGameObject.SetActive(false);
    }

    void ArrangeItems()
    {
        if (menuItems == null || menuItems.Length == 0)
            return;

        int count = menuItems.Length;
        for (int i = 0; i < count; i++)
        {
            float angle = 360f / count * i;
            float rad = angle * Mathf.Deg2Rad;
            float x = Mathf.Cos(rad) * radius;
            float y = Mathf.Sin(rad) * radius;
            menuItems[i].anchoredPosition = new Vector2(x, y);
        }
    }

    void HighlightSelected()
    {
        for (int i = 0; i < menuItems.Length; i++)
        {
            bool isSelected = (i == selectedIndex);
            menuItems[i].localScale = isSelected ? Vector3.one * 1.3f : Vector3.one;
            Image img = menuItems[i].GetComponent<Image>();
            if (img != null)
                img.color = isSelected ? Color.yellow : Color.white;
        }
    }

    void OnClickItem(int index)
    {
        selectedIndex = index;
        HighlightSelected();
        Summon(selectedIndex);
        EventSystem.current.SetSelectedGameObject(null);
    }

    void Summon(int index)
    {
        Debug.Log("召喚：" + menuItems[index].name);
        // 這裡放你的召喚邏輯
    }
}
