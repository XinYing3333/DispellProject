using UnityEngine;

public class SpellRecallUI : MonoBehaviour
{
    [SerializeField] private GameObject hintVisual; // 拖入提示用的 UI 物件

    private void Update()
    {
        if (SpellManager.Instance == null) return;

        // 根據 SpellManager 的狀態切換顯示
        bool shouldShow = SpellManager.Instance.HasActiveSpells;
        
        if (hintVisual.activeSelf != shouldShow)
        {
            hintVisual.SetActive(shouldShow);
        }
    }
}