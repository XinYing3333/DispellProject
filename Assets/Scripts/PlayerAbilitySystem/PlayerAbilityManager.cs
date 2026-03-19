using System.Collections.Generic;
using UnityEngine;
using Player;

namespace AbilitySystem
{
    public class PlayerAbilityManager : MonoBehaviour
    {
        // 原本的字典用來儲存所有實體
        private Dictionary<AbilityType, IAbility> abilities = new Dictionary<AbilityType, IAbility>();
        
        // 👉 新增：用來記錄玩家「目前擁有的能力清單（背包）」
        private List<AbilityType> unlockedAbilities = new List<AbilityType>(); 
        private int currentAbilityIndex = -1;

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


            // var humanPrefab = Resources.Load<GameObject>("Prefabs/Ability/ClonePrefab");
            // AddAbility(AbilityType.HumanThought, new HumanAbility(humanPrefab, transform));
            // SwitchAbility(AbilityType.HumanThought);

            // 註冊你的能力 (這裡以 Bird 為例)
            // AddAbility(AbilityType.BirdThought, new GlideAbility(birdPrefab, transform.GetComponent<Rigidbody>()));
        }

        void Update()
        {
            currentAbility?.Tick();

            // 👉 新增：處理背包切換邏輯 (使用你寫好的 SwitchPressed)
            if (PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.SwitchPressed)
            {
                CycleNextAbility();
            }
        }

        void OnDisable()
        {
            if (PlayerInputHandler.Instance != null)
                PlayerInputHandler.Instance.OnSkill -= UseCurrentAbility;
        }

        // 新增能力時，同時加入解鎖清單
        public void AddAbility(AbilityType type, IAbility ability)
        {
            if (!abilities.ContainsKey(type))
            {
                abilities[type] = ability;
                unlockedAbilities.Add(type);

                // 如果這是獲得的第一個能力，自動裝備
                if (unlockedAbilities.Count == 1)
                {
                    SwitchToAbilityIndex(0);
                }
            }
        }

        // 循環切換到下一個能力
        private void CycleNextAbility()
        {
            if (unlockedAbilities.Count <= 1) return; // 只有一個或沒有時不切換

            int nextIndex = (currentAbilityIndex + 1) % unlockedAbilities.Count;
            SwitchToAbilityIndex(nextIndex);
        }

        private void SwitchToAbilityIndex(int index)
        {
            currentAbilityIndex = index;
            AbilityType typeToSwitch = unlockedAbilities[index];
            SwitchAbility(typeToSwitch);
        }

        public void SwitchAbility(AbilityType type)
        {
            if (!abilities.ContainsKey(type)) return;

            currentAbility?.Deactivate();
            currentAbility = abilities[type];
            currentAbilityType = type;
            currentAbility.Activate();
            
            Debug.Log($"圖騰/能力已切換至: {type}");
            // TODO: 這裡可以觸發一個 Event 讓 UI 更新右下角的圓圈圖示
        }

        public void UseCurrentAbility()
        {
            currentAbility?.Use();
        }
    }
}