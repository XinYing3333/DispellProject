using System.IO;
using UnityEngine;

public static class IncenseCollectionSystem
{
    private static int totalIncense = 0; // 當前香火數量
    private static string savePath = Application.persistentDataPath + "/incenseData.json";

    // 收集香火
    public static void CollectIncense(int amount = 1)
    {
        totalIncense += amount;
        Debug.Log($"收集了 {amount} 個香火，當前總數：{totalIncense}");
        SaveIncense();
    }

    // 獲取當前香火數量
    public static int GetTotalIncense()
    {
        return totalIncense;
    }

    // 清空香火
    public static void ResetIncense()
    {
        totalIncense = 0;
        SaveIncense();
    }

    // 存檔
    public static void SaveIncense()
    {
        File.WriteAllText(savePath, totalIncense.ToString());
    }

    // 加載存檔
    public static void LoadIncense()
    {
        if (File.Exists(savePath))
        {
            string data = File.ReadAllText(savePath);
            int.TryParse(data, out totalIncense);
            Debug.Log($"香火數據已加載：{totalIncense}");
        }
        else
        {
            Debug.Log("未找到香火存檔，從 0 開始");
        }
    }
}