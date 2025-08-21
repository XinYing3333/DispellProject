using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class CollectionSystem
{
    private static Dictionary<string, int> saved   = new Dictionary<string, int>();   // 已存檔
    private static Dictionary<string, int> session = new Dictionary<string, int>();   // 未存檔暫存

    public enum CollectedType { Regular, Special }

    private static string Key(CollectedType t) => t.ToString();

    // ==== 撿取：只記到 session（未存檔不會被記得） ====
    public static void CollectItem(CollectedType itemName, int amount = 1)
    {
        string key = Key(itemName);
        if (!session.ContainsKey(key)) session[key] = 0;
        session[key] += Mathf.Max(1, amount);

#if UNITY_EDITOR
        Debug.Log($"[Collection] 收集到 {key}，本輪暫存數量：{session[key]}");
#endif
    }

    // 是否收過（以「當前遊戲狀態」為準 = saved ∪ session）
    public static bool HasCollected(CollectedType itemName)
    {
        string key = Key(itemName);
        return (saved.ContainsKey(key) && saved[key] > 0) || (session.ContainsKey(key) && session[key] > 0);
    }

    // 目前數量（顯示用 = saved + session）
    public static int GetItemCount(CollectedType itemName)
    {
        string key = Key(itemName);
        int a = saved.ContainsKey(key) ? saved[key] : 0;
        int b = session.ContainsKey(key) ? session[key] : 0;
        return a + b;
    }

    // 當前字典數量（合併後的鍵數）
    public static int GetDictionaryCount()
    {
        HashSet<string> keys = new HashSet<string>(saved.Keys);
        foreach (var k in session.Keys) keys.Add(k);
        return keys.Count;
    }

    // 取得「當前顯示」的合併資料（saved + session）
    public static Dictionary<string, int> GetAllCollectedItems()
    {
        Dictionary<string, int> merged = new Dictionary<string, int>(saved);
        foreach (var kv in session)
        {
            if (!merged.ContainsKey(kv.Key)) merged[kv.Key] = 0;
            merged[kv.Key] += kv.Value;
        }
        return merged;
    }

    // 清空全部並保存（等於新遊戲）
    public static void ClearCollection()
    {
        saved.Clear();
        session.Clear();
        SaveCollection(); // 存一個空的
        Debug.Log("庫存已清空");
    }

    // ==== 存檔：把 session 合併進 saved，然後只把 saved 落地到 JSON ====
    public static void SaveCollection()
    {
        foreach (var kv in session)
        {
            if (!saved.ContainsKey(kv.Key)) saved[kv.Key] = 0;
            saved[kv.Key] += kv.Value;
        }
        session.Clear();

        string savePath = Application.persistentDataPath + "/collectionData.json";
        string json = JsonUtility.ToJson(new Serialization<string, int>(saved));
        File.WriteAllText(savePath, json);
        Debug.Log("收集數據已保存：" + savePath);
    }

    // ==== 讀檔：載入 saved，並清空 session ====
    public static void LoadCollection()
    {
        string savePath = Application.persistentDataPath + "/collectionData.json";
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            saved = JsonUtility.FromJson<Serialization<string, int>>(json).ToDictionary();
            session.Clear();
            Debug.Log("收集數據已加載");
        }
        else
        {
            saved.Clear();
            session.Clear();
            Debug.Log("未找到存檔，開始新遊戲");
        }
    }

    // —— 可選：回到上次存檔（丟棄未存的收集）——
    public static void RevertToLastSave()
    {
        session.Clear();
    }
}

// ==== 你的序列化輔助類（保留原樣） ====
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
