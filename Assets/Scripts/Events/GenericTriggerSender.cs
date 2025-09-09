using UnityEngine;
using Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider))]
public class GenericTriggerSender : MonoBehaviour
{
    [SerializeField] private TriggerEventData triggerEventData;
    [SerializeField] private bool disableAfterTrigger = false;
    [SerializeField] private float disableDelay = 0f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        TriggerEvent();

        if (disableAfterTrigger)
        {
            Invoke(nameof(DisableSelf), disableDelay);
        }
    }

    private void DisableSelf()
    {
        gameObject.SetActive(false);
    }

    private void TriggerEvent()
    {
        switch (triggerEventData.eventType)
        {
            case TriggerEventData.EventType.ChangeCamera:
                EventBus<ChangeCameraEvent>.Publish(new ChangeCameraEvent(triggerEventData.cameraToActivate));
                break;

            case TriggerEventData.EventType.ShowUIText:
                EventBus<TriggerUITextEvent>.Publish(new TriggerUITextEvent(triggerEventData.uiText, triggerEventData.displayTime));
                break;
            
            case TriggerEventData.EventType.OpenGameObject:
                EventBus<OpenObjectEvent>.Publish(new OpenObjectEvent(triggerEventData.objectToOpen));
                break;

            /*case TriggerEventData.EventType.PlaySound:
                EventBus<PlaySoundEvent>.Publish(new PlaySoundEvent(triggerEventData.soundToPlay));
                break;

            case TriggerEventData.EventType.GainPower:
                EventBus<GainPowerEvent>.Publish(new GainPowerEvent());
                break;#1#*/

            default:
                Debug.LogWarning("Unknown event type triggered.");
                break;
        }
    }
}
