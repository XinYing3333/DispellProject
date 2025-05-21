using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class CollectionSystem
{
    private static Dictionary<string, int> collectedItems = new Dictionary<string, int>();

    public enum CollectedType
    {
        Regular,
        Special
    }

    // 收集物品
    public static void CollectItem(CollectedType itemName)
    {
        string key = itemName.ToString();

        if (collectedItems.ContainsKey(key))
        {
            collectedItems[key]++;
        }
        else
        {
            collectedItems[key] = 1;
        }

        Debug.Log($"收集到 {key}，數量：{collectedItems[key]}");
    }

    /*public static void UseItem()
    {
        string itemToUse = GetFirstUsableItem();

        if (itemToUse != null)
        {
            collectedItems[itemToUse]--;
            Debug.Log($"使用 {itemToUse}，剩餘數量：{collectedItems[itemToUse]}");

            if (collectedItems[itemToUse] == 0)
            {
                collectedItems.Remove(itemToUse);
                Debug.Log($"{itemToUse} 已用完，從背包移除！");
            }
        }
        else
        {
            Debug.Log("沒有可用的物品！");
        }
    }

    public static string GetFirstUsableItem()
    {
        return collectedItems.FirstOrDefault(item => item.Value > 0).Key;
    }*/

    public static bool HasCollected(CollectedType itemName)
    {
        return collectedItems.ContainsKey(itemName.ToString());
    }

    public static int GetItemCount(CollectedType itemName)
    {
        return collectedItems.TryGetValue(itemName.ToString(), out int count) ? count : 0;
    }

    public static int GetDictionaryCount()
    {
        return collectedItems.Count;
    }

    public static Dictionary<string, int> GetAllCollectedItems()
    {
        return new Dictionary<string, int>(collectedItems);
    }

    public static void ClearCollection()
    {
        collectedItems.Clear();
        SaveCollection();
        Debug.Log("庫存已清空");
    }

    public static void SaveCollection()
    {
        string savePath = Application.persistentDataPath + "/collectionData.json";
        string json = JsonUtility.ToJson(new Serialization<string, int>(collectedItems));
        File.WriteAllText(savePath, json);
        Debug.Log("收集數據已保存：" + savePath);
    }

    public static void LoadCollection()
    {
        string savePath = Application.persistentDataPath + "/collectionData.json";
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            collectedItems = JsonUtility.FromJson<Serialization<string, int>>(json).ToDictionary();
            Debug.Log("收集數據已加載");
        }
        else
        {
            Debug.Log("未找到存檔，開始新遊戲");
        }
    }
}

// ==== 只保留序列化輔助類 ====
[System.Serializable]
public class Serialization<TKey, TValue>
{
    public List<TKey> keys;
    public List<TValue> values;

    public Serialization(Dictionary<TKey, TValue> dict)
    {
        keys = new List<TKey>(dict.Keys);
        values = new List<TValue>(dict.Values);
    }

    public Dictionary<TKey, TValue> ToDictionary()
    {
        Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
        for (int i = 0; i < keys.Count; i++)
        {
            dict[keys[i]] = values[i];
        }
        return dict;
    }
}
