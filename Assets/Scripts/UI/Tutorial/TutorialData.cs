using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;
using DefaultNamespace.ControlSheme;
using DefaultNamespace.Tutorial;

[CreateAssetMenu(fileName = "NewTutorialData", menuName = "UI/TutorialData")]
public class TutorialData : ScriptableObject
{
    [Header("顯示內容")]
    public string actionName;        // 教學標題 (例如: 閃避)
    [TextArea] public string description; // 描述文本 (例如: 在敵人攻擊瞬間按下按鈕)
    public VideoClip tutorialVideo;  // 示範短片

    [Header("顯示圖示 (與 BindingLibrary 對接)")]
    [Tooltip("這裡填入 Action 名稱，用於生成 UI 上的按鍵圖示")]
    public List<ActionName> requiredInputActions; 

    [Header("達成需求 (與 InputHandler/EventBus 對接)")]
    [Tooltip("玩家必須達成此清單中所有的 Key 才會打勾。可以包含 Action 名稱、State 名稱或 Event 名稱")]
    public List<TutorialRequirementType> requiredRequirements;
    
    [Header("設定")]
    public float displayDuration = 6.0f; // 若非強制偵測模式的自動消失時間
}