using System;
using System.Collections.Generic;

public static class SpellEffectFactory 
{
    private static Dictionary<SpellType, ISpellEffect> effectMap = new Dictionary<SpellType, ISpellEffect>()
    {
        { SpellType.AttackSpell, new AttackSpell() },
    };

    public static ISpellEffect GetEffect(SpellType spellType)
    {
        return effectMap.ContainsKey(spellType) ? effectMap[spellType] : null;
    }
}
