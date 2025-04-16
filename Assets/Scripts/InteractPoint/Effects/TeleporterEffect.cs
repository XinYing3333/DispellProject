using UnityEngine;

public class TeleporterEffect : InteractionEffect
{
    public GameObject triggerGate;
    public GameObject untriggerGate;

    public InteractionTriggerType triggerType;//觸發條件
    public bool objectStateWhenTrigger = true; // 離開時開啟，進入時關閉
    
    public SpawnType requiredSpawnType; 


    public override void ExecuteEffect(InteractionPoint interactionPoint, SpawnObject spawnObject, InteractionTriggerType triggerType)
    {
        if (spawnObject.spawnType != requiredSpawnType) return;

        if (triggerGate != null && untriggerGate != null)
        {
            if (this.triggerType == triggerType)
            {
                triggerGate.SetActive(objectStateWhenTrigger);
                untriggerGate.SetActive(!objectStateWhenTrigger);
            }
            else
            {
                untriggerGate.SetActive(objectStateWhenTrigger);
                triggerGate.SetActive(!objectStateWhenTrigger);
            }
            
        }
    }
}