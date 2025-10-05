using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SimpleRadialToggle : MonoBehaviour
{
    [Header("要顯示/隱藏的環狀選單")]
    public GameObject radialMenu; // 拖入 Hierarchy 中的 UI Panel

    [Header("環狀選單內的按鈕")]
    public RectTransform[] menuItems; // 手動排好位置的按鈕

    private int selectedIndex = 0;
    private bool isOpen = false;

    private void Start()
    {
        if (radialMenu == null)
        {
            Debug.LogError("請在 Inspector 指定 radialMenu！");
            return;
        }

        // 一開始先隱藏選單
        radialMenu.SetActive(false);

        // 加入按鈕點擊事件
        for (int i = 0; i < menuItems.Length; i++)
        {
            int index = i;
            Button btn = menuItems[i].GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnClickItem(index));
            }
        }

        HighlightSelected();
    }

    private void Update()
    {
        // 切換選單開關（Tab 鍵）
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isOpen = !isOpen;
            radialMenu.SetActive(isOpen);
            if (isOpen)
                HighlightSelected();
        }

        if (!isOpen)
            return;

        // 左右切換選項（方向鍵）
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

        // 按下空白鍵召喚
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Summon(selectedIndex);
        }
    }

    // 高亮選中項目
    private void HighlightSelected()
    {
        for (int i = 0; i < menuItems.Length; i++)
        {
            bool isSelected = (i == selectedIndex);

            // 放大到 0.35，沒選到則回到正常
            menuItems[i].localScale = isSelected ? Vector3.one * 0.35f : Vector3.one;

            // 改變顏色
            Image img = menuItems[i].GetComponent<Image>();
            if (img != null)
            {
                img.color = isSelected ? Color.yellow : Color.white;
            }
        }
    }

    // 滑鼠點擊選項
    private void OnClickItem(int index)
    {
        selectedIndex = index;
        HighlightSelected();
        Summon(index);
        EventSystem.current.SetSelectedGameObject(null); // 避免搶奪鍵盤焦點
    }

    // 召喚處理
    private void Summon(int index)
    {
        string summonName = menuItems[index].name;
        Debug.Log("召喚了：" + summonName);

        // TODO: 你自己的召喚邏輯可以放這裡
    }
}
