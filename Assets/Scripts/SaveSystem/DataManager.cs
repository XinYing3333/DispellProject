using UnityEngine;
using System;
using System.Collections.Generic;

[DefaultExecutionOrder(-2000)]
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    [Serializable]
    public class GameData
    {
        public int hp;
        // 替換原本的 int，改用序列化結構存儲所有類型的收集數量
        public CollectionSerialization collection = new();
        
        public string lastCheckpointId;
        public string lastSceneName;
        public List<string> triggeredTutorialIds = new();
        // 核心：記錄所有已撿取物件的唯一 ID
        public List<string> collectedThoughtIds = new();
        public List<string> sessionCollectedIds = new List<string>();
        
        public List<string> defeatedEnemyIds = new();
        
        public Vector3 fallbackPos;
        public Vector3 fallbackEuler;
    }

    [Serializable]
    public class CollectionSerialization
    {
        public List<string> keys = new();
        public List<int> values = new();
        public void FromDict(Dictionary<string, int> dict)
        {
            keys.Clear(); values.Clear();
            foreach(var kv in dict) { keys.Add(kv.Key); values.Add(kv.Value); }
        }
        public Dictionary<string, int> ToDict()
        {
            var dict = new Dictionary<string, int>();
            for (int i = 0; i < keys.Count; i++) dict[keys[i]] = values[i];
            return dict;
        }
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
    public bool IsThoughtCollected(string id) 
    {
        // 同時檢查正式存檔與本次進度的暫存
        return gameData.collectedThoughtIds.Contains(id) || gameData.sessionCollectedIds.Contains(id);
    }
}