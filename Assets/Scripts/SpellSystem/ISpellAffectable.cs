using UnityEngine;

namespace SpellSystem
{
    public interface ISpellAffectable
    {
        // 執行法術效果
        void OnSpellHit(SpellType spellType, Vector3 hitPoint);
    
        // 撤銷法術效果
        void OnSpellRecall();
    }
}