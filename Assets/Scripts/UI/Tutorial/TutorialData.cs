using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;
using DefaultNamespace.ControlSheme;
using DefaultNamespace.Tutorial;

[CreateAssetMenu(fileName = "NewTutorialData", menuName = "UI/TutorialData")]
public class TutorialData : ScriptableObject
{
    [Header("顯示內容 (Localized)")]
    public string actionNameCH;
    public string actionNameEN;
    
    [TextArea] public string descriptionCH;
    [TextArea] public string descriptionEN;

    [Header("媒體與邏輯 (不變)")]
    public VideoClip tutorialVideo;
    public List<ActionName> requiredInputActions; 
    public List<TutorialRequirementType> requiredRequirements;
    public float displayDuration = 6.0f;

    // 取得當前語言內容
    public (string title, string desc) GetContent(UI.Localization.Language lang)
    {
        if (lang == UI.Localization.Language.zh)
            return (actionNameCH, descriptionCH);
        
        return (actionNameEN, descriptionEN);
    }
}

