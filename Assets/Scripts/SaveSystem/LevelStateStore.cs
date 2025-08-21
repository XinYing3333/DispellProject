using System.Collections.Generic;
using UnityEngine;

public class LevelStateStore : MonoBehaviour
{
    public static LevelStateStore Instance { get; private set; }

    private HashSet<string> saved   = new HashSet<string>(); // 已存檔
    private HashSet<string> session = new HashSet<string>(); // 本輪暫存（未存檔）

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // —— 念頭相關 ——
    public void MarkCollectedSession(string spawnId)
    {
        if (!string.IsNullOrEmpty(spawnId)) session.Add(spawnId);
    }

    public bool IsCollectedNow(string spawnId) => saved.Contains(spawnId) || session.Contains(spawnId);

    // —— 存檔/讀檔/回檔 對念頭層的操作 ——
    public void ApplyFromSaveData(SaveData data)
    {
        saved = new HashSet<string>(data?.collectedSpawnIds ?? new List<string>());
        session.Clear();
    }

    public void WriteToSaveData(SaveData data)
    {
        saved.UnionWith(session);
        session.Clear();
        data.collectedSpawnIds = new List<string>(saved);
    }

    public void RevertToLastSave()
    {
        session.Clear();
    }

    // 可選：開新檔用
    public void ClearAll()
    {
        saved.Clear();
        session.Clear();
    }
}