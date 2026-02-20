using UnityEngine;

// 圖騰類型枚舉
public enum TotemType { None, Attack, Stun, GiantBird }

[CreateAssetMenu(fileName = "New Totem", menuName = "Game/Totem Data")]
public class TotemData : ScriptableObject
{
    public TotemType type;
    public string totemName;
    public Sprite icon; // 用於 UI 顯示
    public float cooldown = 1.0f;
    
    [TextArea]
    public string description;
}