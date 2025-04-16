using UnityEngine;

public class AnimatorTriggerEffect : InteractionEffect
{
    public Animator objectAnimator;
    public InteractionTriggerType triggerType = InteractionTriggerType.OnEnter; //觸發條件

    public SpawnType exceptSpawnType; 

    
    public override void ExecuteEffect(InteractionPoint interactionPoint, SpawnObject spawnObject, InteractionTriggerType triggerType)
    {
        if (spawnObject.spawnType == exceptSpawnType) return;

        
        if (objectAnimator != null )//&& this.triggerType == triggerType)
        {
            if (this.triggerType == triggerType)
            {
                objectAnimator.SetTrigger("isTrigger");
            }
        }
    }
}