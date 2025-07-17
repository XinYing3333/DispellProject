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

            AddAbility(AbilityType.HumanThought , new HumanAbility(GameObject.FindGameObjectWithTag("Clone"),transform));
            SwitchAbility(AbilityType.HumanThought);

            AddAbility(AbilityType.BirdThought , new GlideAbility(Resources.Load<GameObject>("Prefabs/Ability/BirdPrefab"), transform.GetComponent<Rigidbody>()));
            //SwitchAbility(AbilityType.BirdThought);

            AddAbility(AbilityType.ElephantThought,
                new ElephantAbility(Resources.Load<GameObject>("Prefabs/Ability/ElephantPrefab"), transform,
                    transform.gameObject.GetComponent<PlayerMovement>()));
            //SwitchAbility(AbilityType.ElephantThought);
        }

        void Update()
        {
            currentAbility?.Tick();
            
            //================ 測試用記得改 ============================
            if(Input.GetKeyDown(KeyCode.Q))
            {
                switch (currentAbilityType)
                {
                    case AbilityType.HumanThought:
                        SwitchAbility(AbilityType.BirdThought);
                        break;
                    case AbilityType.BirdThought:
                        SwitchAbility(AbilityType.ElephantThought);
                        break;
                    case AbilityType.ElephantThought:
                        SwitchAbility(AbilityType.HumanThought);
                        break;
                }
            }
            //=========================================================
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
            Debug.Log($"Ability switched to {type}");
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