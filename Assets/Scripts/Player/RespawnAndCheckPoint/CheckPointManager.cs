using System;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Serializable]
    public struct SaveStruct
    {
        public string scene;
        public string checkpointId;   // 用唯一ID
        public Vector3 fallbackPos;   // 地圖改動找不到ID時的備援
        public Vector3 fallbackEuler;
    }

    private const string LAST_KEY   = "LAST_CHECKPOINT_V2";
    private const string ACT_PREFIX = "CP_ACT_"; // 是否曾觸發過某 checkpoint

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---- 查詢/讀取 ----
    public bool HasSavedCheckpoint() => PlayerPrefs.HasKey(LAST_KEY);

    public bool TryLoadSavedCheckpoint(out SaveStruct data)
    {
        data = default;
        if (!PlayerPrefs.HasKey(LAST_KEY)) return false;
        try
        {
            data = JsonUtility.FromJson<SaveStruct>(PlayerPrefs.GetString(LAST_KEY));
            return !string.IsNullOrEmpty(data.scene) && !string.IsNullOrEmpty(data.checkpointId);
        }
        catch { return false; }
    }

    public bool IsCheckpointActivated(string checkpointId)
    {
        return PlayerPrefs.GetInt(ACT_PREFIX + checkpointId, 0) == 1;
    }

    // ---- 寫入（只在第一次踏入該ID時生效） ----
    public bool SaveCheckpointFirstTime(string checkpointId, Transform spawn)
    {
        if (IsCheckpointActivated(checkpointId)) return false; // 已經記過就不覆寫

        var data = new SaveStruct
        {
            scene        = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            checkpointId = checkpointId,
            fallbackPos  = spawn.position,
            fallbackEuler= spawn.rotation.eulerAngles
        };

        var json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(LAST_KEY, json);
        PlayerPrefs.SetInt(ACT_PREFIX + checkpointId, 1);
        PlayerPrefs.Save();

        Debug.Log($"[Checkpoint] First time reach: {data.scene}:{checkpointId} -> saved to PlayerPrefs");
        return true;
    }

    // ---- 清除 ----
    public void ClearLastCheckpoint()
    {
        PlayerPrefs.DeleteKey(LAST_KEY);
    }

    public void ClearAllActivationFlags()
    {
        // 若要全清，建議你另外維護一份所有 checkpointId 清單再逐一刪除
        Debug.LogWarning("ClearAllActivationFlags: 需要你自行實作遍歷所有ID刪除 ACT_PREFIX。");
    }
}
