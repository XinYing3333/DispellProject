using System;
using System.Collections.Generic;
using UnityEngine;
using Player; 

public class SpellInventoryController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InteractionController interactionController;

    [Header("Inventory")]
    [Tooltip("玩家目前持有的法術清單")]
    [SerializeField] private List<SpellType> unlockedSpells = new List<SpellType>();

    private int _currentIndex = 0;

    // 供 UI 系統監聽的事件，傳遞當前選擇的法術
    public event Action<SpellType> OnSpellChanged;

    private void Start()
    {
        if (unlockedSpells.Count == 0)
        {
            Debug.LogWarning("[SpellInventory] 法術清單為空，填入預設值。");
            unlockedSpells.Add(SpellType.AttackSpell); 
            unlockedSpells.Add(SpellType.StopSpell); 
            unlockedSpells.Add(SpellType.BirdSpell); 
        }
        
        // 初始化狀態
        UpdateSpellSelection();
    }

    private void Update()
    {
        // 阻斷條件：無輸入源或輸入被鎖定
        if (PlayerInputHandler.Instance == null || PlayerInputHandler.Instance.InputLock) return;

        // 讀取 Shoulder 按鍵輸入（使用你已定義的屬性）
        if (PlayerInputHandler.Instance.SettingLeftPressed)
        {
            CycleSpell(-1);
        }
        else if (PlayerInputHandler.Instance.SettingRightPressed)
        {
            CycleSpell(1);
        }
    }

    private void CycleSpell(int direction)
    {
        if (unlockedSpells.Count <= 1) return;

        _currentIndex += direction;

        // 處理陣列索引循環 (Wrap-around)
        if (_currentIndex < 0)
        {
            _currentIndex = unlockedSpells.Count - 1;
        }
        else if (_currentIndex >= unlockedSpells.Count)
        {
            _currentIndex = 0;
        }

        UpdateSpellSelection();
    }

    private void UpdateSpellSelection()
    {
        SpellType selectedSpell = unlockedSpells[_currentIndex];
        
        // 1. 通知執行層變更物理投擲的實體屬性
        if (interactionController != null)
        {
            interactionController.SetSpellType(selectedSpell);
        }

        // 2. 廣播事件，觸發 UI 更新輪盤畫面
        OnSpellChanged?.Invoke(selectedSpell);
    }

    // 擴充接口：遊戲進程中獲得新法術時呼叫
    public void UnlockSpell(SpellType newSpell)
    {
        if (!unlockedSpells.Contains(newSpell))
        {
            unlockedSpells.Add(newSpell);
        }
    }
    
    // 在 SpellInventoryController.cs 裡面加入：
    public List<SpellType> GetUnlockedSpells()
    {
        return unlockedSpells;
    }
}