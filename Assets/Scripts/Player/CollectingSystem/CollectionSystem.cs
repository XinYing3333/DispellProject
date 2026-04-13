using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 收集系統：介接 DataManager，提供暫存 (session) 與持久化數據的統一存取與消耗邏輯。
/// </summary>
public static class CollectionSystem
{
    private static Dictionary<string, int> session = new Dictionary<string, int>();

    // 宣告事件：參數 = 物品類型, 當前總數量
    public static event System.Action<CollectedType, int> OnCollected;
    public enum CollectedType { Though, EnemyThough, StopSignThough, Offering }

    private static string Key(CollectedType t) => t.ToString();

    // ==== 內部輔助：取得與設定 DataManager 內的持久化字典 ====
    private static Dictionary<string, int> GetSavedDict() => DataManager.Instance.gameData.collection.ToDict();
    
    private static void SetSavedDict(Dictionary<string, int> dict)
    {
        DataManager.Instance.gameData.collection.FromDict(dict);
    }

    // ==== 撿取：只記到 session ====
    public static void CollectItem(CollectedType itemName, int amount = 1)
    {
        string key = Key(itemName);
        if (!session.ContainsKey(key)) session[key] = 0;
        session[key] += Mathf.Max(1, amount);

#if UNITY_EDITOR
        Debug.Log($"[Collection] 收集到 {key}，本輪暫存數量：{session[key]}");
#endif
        OnCollected?.Invoke(itemName, GetItemCount(itemName));
    }

    // 目前數量（顯示用 = saved + session）
    public static int GetItemCount(CollectedType itemName)
    {
        string key = Key(itemName);
        var saved = GetSavedDict();
        int a = saved.ContainsKey(key) ? saved[key] : 0;
        int b = session.ContainsKey(key) ? session[key] : 0;
        return a + b;
    }

    // ✅ 消耗：先扣 session，再扣 saved；不足則失敗且不改變任何數值
    public static bool TryConsumeItem(CollectedType itemName, int amount)
    {
        amount = Mathf.Max(1, amount);

        int total = GetItemCount(itemName);
        if (total < amount) return false;

        string key = Key(itemName);
        var saved = GetSavedDict();

        int sess = session.ContainsKey(key) ? session[key] : 0;
        int sav  = saved.ContainsKey(key)   ? saved[key]   : 0;

        int remaining = amount;

        // 1. 扣 session
        if (sess > 0)
        {
            int take = Mathf.Min(sess, remaining);
            sess -= take;
            remaining -= take;
        }

        // 2. 扣 saved
        if (remaining > 0 && sav > 0)
        {
            int take = Mathf.Min(sav, remaining);
            sav -= take;
            remaining -= take;
        }

        // 必須完全扣除
        if (remaining != 0) return false;

        // 更新 session 字典
        if (sess <= 0) session.Remove(key);
        else session[key] = sess;

        // 更新 saved 字典並回寫至 DataManager
        if (sav <= 0) saved.Remove(key);
        else saved[key] = sav;
        
        SetSavedDict(saved);

#if UNITY_EDITOR
        Debug.Log($"[Collection] 消耗 {key} x{amount}，剩餘：{GetItemCount(itemName)}");
#endif
        OnCollected?.Invoke(itemName, GetItemCount(itemName));
        return true;
    }

    // ==== 存檔：把 session 合併進 DataManager 的持久化數據 ====
    public static void SaveCollection()
    {
        var data = DataManager.Instance.gameData;
    
        // 1. 搬運 ID 清單：將本次 Session 撿到的 ID 正式併入存檔
        foreach (var id in DataManager.Instance.gameData.sessionCollectedIds)
        {
            if (!data.collectedThoughtIds.Contains(id))
                data.collectedThoughtIds.Add(id);
        }
        DataManager.Instance.gameData.sessionCollectedIds.Clear();

        // 2. 原有的數量合併邏輯...
        var dict = data.collection.ToDict();
        foreach (var kv in session)
        {
            if (!dict.ContainsKey(kv.Key)) dict[kv.Key] = 0;
            dict[kv.Key] += kv.Value;
        }
        session.Clear();
        data.collection.FromDict(dict);

        DataManager.Instance.SaveToDisk();
    }
    
    // —— 回到上次存檔（丟棄未存的收集，用於死亡重載） ——
    public static void RevertToLastSave()
    {
        session.Clear();
    }
}