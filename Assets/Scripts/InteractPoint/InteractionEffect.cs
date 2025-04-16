using UnityEngine;

public abstract class InteractionEffect : MonoBehaviour
{
    public abstract void ExecuteEffect(InteractionPoint interactionPoint, SpawnObject spawnObject, InteractionTriggerType triggerType);
}
