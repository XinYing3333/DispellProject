using UnityEngine;

namespace SpellSystem
{
    public interface ISpellAffectable
    {
        void OnSpellHit(SpellType spellType, Vector3 hitPoint);
    }
}