using UnityEngine;

public abstract class InteractionEffect : MonoBehaviour
{
    public abstract void ExecuteEffect(InteractionPoint interactionPoint, ThoughtObject thoughtObject, InteractionTriggerType triggerType);
}
