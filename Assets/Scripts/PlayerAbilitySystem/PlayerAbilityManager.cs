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

        private GameObject elephantPrefab;
        private GameObject birdPrefab;

        void Start()
        {
            if (PlayerInputHandler.Instance != null)
                PlayerInputHandler.Instance.OnSkill += UseCurrentAbility;

            // Instantiate Bird and Elephant prefabs as children of the player
            birdPrefab = Instantiate(Resources.Load<GameObject>("Prefabs/Ability/BirdPrefab"), transform);
            birdPrefab.SetActive(false);

            elephantPrefab = Instantiate(Resources.Load<GameObject>("Prefabs/Ability/ElephantPrefab"), transform);
            elephantPrefab.SetActive(false);


            var humanPrefab = Resources.Load<GameObject>("Prefabs/Ability/ClonePrefab");
            AddAbility(AbilityType.HumanThought, new HumanAbility(humanPrefab, transform));
            SwitchAbility(AbilityType.HumanThought);

            AddAbility(AbilityType.BirdThought, new GlideAbility(birdPrefab, transform.GetComponent<Rigidbody>()));
            //SwitchAbility(AbilityType.BirdThought);

            AddAbility(AbilityType.ElephantThought,
                new ElephantAbility(elephantPrefab, transform,
                    transform.gameObject.GetComponent<PlayerMovement>()));
            //SwitchAbility(AbilityType.ElephantThought);
        }

        void Update()
        {
            currentAbility?.Tick();

            //================ 測試用記得改 ============================
            if (Input.GetKeyDown(KeyCode.Q))
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