using System;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;

namespace Player
{
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance;
        
        [SerializeField]private TMP_Text fruitCountText;
        [SerializeField]private TMP_Text incenseCountText;

        private int totalFruitCount; 
        
        void Awake()
        {
            Instance = this;
            DeleteFruitInventory();
            totalFruitCount = PlayerPrefs.GetInt("totalFruitCount");
        }

        void Update()
        {
           fruitCountText.text = $"Total Fruits: {totalFruitCount.ToString()}";
           incenseCountText.text = $"Total Incenses: {IncenseCollectionSystem.GetTotalIncense().ToString()}";
            if (Input.GetKeyDown(KeyCode.F1))
            {
                DeleteInventory();
            }
        }

        public bool GetFruitCount()
        {
            if (totalFruitCount == 3)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public void GetFruit(int fruitCount)
        {
            totalFruitCount += fruitCount;
            PlayerPrefs.SetInt("totalFruitCount", totalFruitCount);
        }
        
        public void DeleteFruitInventory()
        {
            totalFruitCount = 0;
            PlayerPrefs.DeleteKey("totalFruitCount");
        }
        
        public void DeleteInventory()
        {
            totalFruitCount = 0;
            IncenseCollectionSystem.ResetIncense();
            PlayerPrefs.DeleteKey("totalFruitCount");
        }
        
    }
}