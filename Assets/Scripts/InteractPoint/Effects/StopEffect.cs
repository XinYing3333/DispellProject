using UnityEngine;

public class StopEffect : InteractionEffect
{
    public GameObject objectCar;

    public InteractionTriggerType triggerType;//觸發條件
    
    public SpawnType requiredSpawnType; 


    public override void ExecuteEffect(InteractionPoint interactionPoint, SpawnObject spawnObject, InteractionTriggerType triggerType)
    {
        if (spawnObject.spawnType != requiredSpawnType) return;

        if (objectCar != null)
        {
            if (this.triggerType == triggerType)
            {
                Debug.Log("Stop Effect");
                objectCar.GetComponent<MovingPlatform>().enabled = false;
            }
            else if (this.triggerType == InteractionTriggerType.OnExit)
            {
                objectCar.GetComponent<MovingPlatform>().enabled = true;
            }
            
        }
    }
}