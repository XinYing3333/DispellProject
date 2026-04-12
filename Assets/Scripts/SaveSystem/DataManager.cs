using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

[DefaultExecutionOrder(-2000)] // 確保最早啟動
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    [Serializable]
    public class GameData
    {
        // 玩家狀態
        public int hp;
        public int collectedIdeasCount;
        public List<string> unlockedSpells = new();
        
        // 進度狀態 (ID 系統)
        public string lastCheckpointId;
        public string lastSceneName;
        public List<string> triggeredTutorialIds = new();
        public List<string> collectedThoughtIds = new();
        public List<string> defeatedEnemyIds = new();
        
        // 備援座標
        public Vector3 fallbackPos;
        public Vector3 fallbackEuler;
    }

    public GameData gameData = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFromDisk();
    }

    public void SaveToDisk()
    {
        string json = JsonUtility.ToJson(gameData);
        PlayerPrefs.SetString("SaveSlot_1", json);
        PlayerPrefs.Save();
    }

    public void LoadFromDisk()
    {
        if (PlayerPrefs.HasKey("SaveSlot_1"))
        {
            string json = PlayerPrefs.GetString("SaveSlot_1");
            gameData = JsonUtility.FromJson<GameData>(json);
        }
    }

    // 提供給各系統檢查進度的簡單介面
    public bool IsIdTriggered(string id) => gameData.triggeredTutorialIds.Contains(id) || gameData.collectedThoughtIds.Contains(id);
}