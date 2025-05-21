using System;
using UnityEngine;
using System.Collections.Generic;
using Player;

namespace AbilitySystem
{
    public class PlayerAbilityManager : MonoBehaviour
    {
        private Dictionary<AbilityType, IAbility> abilities = new Dictionary<AbilityType, IAbility>();
        private IAbility currentAbility;
        private AbilityType? currentAbilityType = null;

        void Start()
        {
            if (PlayerInputHandler.Instance != null)
                PlayerInputHandler.Instance.OnSkill += UseCurrentAbility;
            
            AddAbility(AbilityType.HumanThought , new HumanAbility(Resources.Load<GameObject>("Prefabs/Ability/ClonePrefab"),transform));
            SwitchAbility(AbilityType.HumanThought);
        }

        void OnDisable()
        {
            if (PlayerInputHandler.Instance != null)
                PlayerInputHandler.Instance.OnSkill -= UseCurrentAbility;
        }

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
            Debug.Log(currentAbility);
            currentAbility?.Use();
        }
        
        public void RemoveAbility(AbilityType type)
        {
            abilities.Remove(type);
        }
    }
}