using System;
using System.Collections.Generic;

public static class SpellEffectFactory 
{
    private static Dictionary<SpellType, ISpellEffect> effectMap = new Dictionary<SpellType, ISpellEffect>()
    {
        { SpellType.RotateSpell, new RotateSpell() },
        { SpellType.AttackSpell, new AttackSpell() },
        { SpellType.ElectricBullet, new RotateSpell() }
    };

    public static ISpellEffect GetEffect(SpellType spellType)
    {
        return effectMap.ContainsKey(spellType) ? effectMap[spellType] : null;
    }
}
