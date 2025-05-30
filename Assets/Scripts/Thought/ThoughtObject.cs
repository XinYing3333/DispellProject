using UnityEngine;

public class ThoughtObject : MonoBehaviour
{
    public bool isCollectable;
    public CollectionSystem.CollectedType collectedType;
    
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
