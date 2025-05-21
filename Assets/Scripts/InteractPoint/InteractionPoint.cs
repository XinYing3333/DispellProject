using UnityEngine;

public class InteractionPoint : MonoBehaviour
{
    public InteractionEffect[] interactionEffects;

    public void TriggerInteraction(Spell spell, InteractionTriggerType triggerType)
    {
        if (interactionEffects.Length == 0) return;

        foreach (var effect in interactionEffects)
        {
            effect.ExecuteEffect(this, spell, triggerType);
        }
    }
}