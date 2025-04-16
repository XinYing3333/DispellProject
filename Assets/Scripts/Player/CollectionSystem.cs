using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class CollectionSystem
{
    private static Dictionary<string, CollectedItemData> collectedItems = new Dictionary<string, CollectedItemData>();

    // 收集物品
    public static void CollectItem(SpawnType itemName, Quaternion rotation)
    {
        string key = itemName.ToString();
        Vector3 eulerRotation = rotation.eulerAngles;

        if (collectedItems.ContainsKey(key))
        {
            collectedItems[key].count++;
            collectedItems[key].rotationEuler = eulerRotation; // 更新為最新的旋轉
        }
        else
        {
            collectedItems[key] = new CollectedItemData(1, eulerRotation);
        }

        Debug.Log($"收集到 {key}，數量：{collectedItems[key].count}，朝向：{collectedItems[key].rotationEuler}");
    }

    /*public static Vector3 GetItemRotation(SpawnType itemName)
    {
        string key = itemName.ToString();
        Vector3 eulerRotation = collectedItems[key].rotationEuler;
        return eulerRotation;
    }*/

    public static void UseItem()
    {
        string itemToUse = GetFirstUsableItem();

        if (itemToUse != null)
        {
            CollectedItemData data = collectedItems[itemToUse];
            data.count--;
            Debug.Log($"使用 {itemToUse}，剩餘數量：{data.count}");

            if (data.count == 0)
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
        return collectedItems.FirstOrDefault(item => item.Value.count > 0).Key;
    }

    public static bool HasCollected(SpawnType itemName)
    {
        return collectedItems.ContainsKey(itemName.ToString());
    }

    public static int GetItemCount(SpawnType itemName)
    {
        return collectedItems.TryGetValue(itemName.ToString(), out var data) ? data.count : 0;
    }

    public static int GetDictionaryCount()
    {
        return collectedItems.Count;
    }

    public static Dictionary<string, CollectedItemData> GetAllCollectedItems()
    {
        return new Dictionary<string, CollectedItemData>(collectedItems);
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
        string json = JsonUtility.ToJson(new Serialization<string, CollectedItemData>(collectedItems));
        File.WriteAllText(savePath, json);
        Debug.Log("收集數據已保存：" + savePath);
    }

    public static void LoadCollection()
    {
        string savePath = Application.persistentDataPath + "/collectionData.json";
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            collectedItems = JsonUtility.FromJson<Serialization<string, CollectedItemData>>(json).ToDictionary();
            Debug.Log("收集數據已加載");
        }
        else
        {
            Debug.Log("未找到存檔，開始新遊戲");
        }
    }
}

[System.Serializable]
public class CollectedItemData
{
    public int count;
    public Vector3 rotationEuler;

    public CollectedItemData(int count, Vector3 rotation)
    {
        this.count = count;
        this.rotationEuler = rotation;
    }
}

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
