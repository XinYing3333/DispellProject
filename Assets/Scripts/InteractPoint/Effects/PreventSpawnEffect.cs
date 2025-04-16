using UnityEngine;

public class PreventSpawnEffect : InteractionEffect
{
    public SpawnType requiredSpawnType; 

    public override void ExecuteEffect(InteractionPoint interactionPoint, SpawnObject spawnObject, InteractionTriggerType triggerType)
    {
        if (spawnObject.spawnType != requiredSpawnType) return;
        
        Debug.Log($"[PreventSpawnEffect] 阻止生成物件！");
        spawnObject.gameObject.SetActive(false);  // 停止生成或執行特殊邏輯
    }
}