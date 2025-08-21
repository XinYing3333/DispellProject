using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    // 已「存檔」的念頭 spawnId
    public List<string> collectedSpawnIds = new List<string>();

    // 已「存檔」的物品庫存（Regular/Special…）
    public List<string> invKeys = new List<string>();
    public List<int>    invValues = new List<int>();
}