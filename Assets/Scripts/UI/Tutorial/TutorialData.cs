using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "NewTutorial", menuName = "UI/TutorialData")]
public class TutorialData : ScriptableObject
{
    public string actionName; // 顯示用的標題
    [TextArea] public string description;
    public VideoClip tutorialVideo;
    
    // 改存 Action 的 ID 或名稱，例如 "Jump"
    public List<string> requiredInputActions; 
}