using UnityEngine;

public class SpawnObject : MonoBehaviour
{
    public bool isCollectable;
    public SpawnType spawnType;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<InteractionPoint>(out InteractionPoint interactionPoint))
        {
            interactionPoint.TriggerInteraction(this , InteractionTriggerType.OnEnter);  
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.TryGetComponent<InteractionPoint>(out InteractionPoint interactionPoint))
        {
            interactionPoint.TriggerInteraction(this , InteractionTriggerType.OnStay);  
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent<InteractionPoint>(out InteractionPoint interactionPoint))
        {
            interactionPoint.TriggerInteraction(this , InteractionTriggerType.OnExit);  
        }
    }
    
    public void ApplyEffect(SpellType spellType)
    {
        ISpellEffect effect = SpellEffectFactory.GetEffect(spellType);
        if (effect != null)
        {
            effect.ApplyEffect(this);
        }
        else
        {
            Debug.LogWarning($"{spellType} 沒有對應的效果！");
        }
    }
}
