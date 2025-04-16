using UnityEngine;

public class OpenObjectEffect : InteractionEffect
{
    public GameObject objectToOpen;
    public InteractionTriggerType triggerType = InteractionTriggerType.OnExit; //觸發條件
    public bool objectStateWhenTrigger = true; // 離開時開啟，進入時關閉
    
    public SpawnType requiredSpawnType; 


    public override void ExecuteEffect(InteractionPoint interactionPoint, SpawnObject spawnObject, InteractionTriggerType triggerType)
    {
        if (spawnObject.spawnType != requiredSpawnType) return;

        if (objectToOpen != null )//&& this.triggerType == triggerType)
        {
            if (this.triggerType == triggerType)
            {
                objectToOpen.SetActive(objectStateWhenTrigger);
            }
            else //除了 trigger type 之外的都執行
            {
                objectToOpen.SetActive(!objectStateWhenTrigger);
            }
            
        }
    }
}
