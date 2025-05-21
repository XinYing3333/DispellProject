using UnityEngine;

public class AnimatorEffect : InteractionEffect
{
    public Animator objectAnimator;
    public InteractionTriggerType triggerType = InteractionTriggerType.OnEnter; //觸發條件

    //public SpawnType requiredSpawnType; 

    
    public override void ExecuteEffect(InteractionPoint interactionPoint, Spell spell, InteractionTriggerType triggerType)
    {
        //if (thoughObject.spawnType != requiredSpawnType) return;
        Debug.Log("Animation triggered");
        
        /*if (objectAnimator != null )//&& this.triggerType == triggerType)
        {
            if (this.triggerType == triggerType)
            {
                Debug.Log("Animation triggered");
                objectAnimator.SetBool("isTrue", true);
                spell.gameObject.SetActive(false);  // 停止生成或執行特殊邏輯

            }
            else
            {
                objectAnimator.SetBool("isTrue", false);

            }
            
        }*/
    }
}