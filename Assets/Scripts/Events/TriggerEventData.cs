using UnityEngine;

namespace Events
{
    [CreateAssetMenu(menuName = "Events/TriggerEventData")]
    public class TriggerEventData : ScriptableObject
    {
        //這裏擴充加入新的 Event
        //需要加上對應的 Event 脚本
        public enum EventType { ChangeCamera, ShowUIText, PlaySound, GainPower }

        public EventType eventType;
        public Cinemachine.CinemachineVirtualCamera cameraToActivate;
        [TextArea] public string uiText;
        public AudioClip soundToPlay;
        public float displayTime = 3f;

    }
}