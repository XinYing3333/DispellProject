using UnityEngine;
using System.Collections.Generic;

namespace AbilitySystem
{
    public class PlayerAbilityManager : MonoBehaviour
    {
        private Dictionary<AbilityType, IAbility> abilities = new Dictionary<AbilityType, IAbility>();
        private IAbility currentAbility;
        private AbilityType? currentAbilityType = null;

        public void AddAbility(AbilityType type, IAbility ability)
        {
            if (!abilities.ContainsKey(type))
                abilities[type] = ability;
        }

        public void SwitchAbility(AbilityType type)
        {
            if (!abilities.ContainsKey(type)) return;

            currentAbility?.Deactivate();
            currentAbility = abilities[type];
            currentAbilityType = type;
            currentAbility.Activate();
        }

        public void UseCurrentAbility()
        {
            currentAbility?.Use();
        }
        
        public void RemoveAbility(AbilityType type)
        {
            abilities.Remove(type);
        }
    }
}