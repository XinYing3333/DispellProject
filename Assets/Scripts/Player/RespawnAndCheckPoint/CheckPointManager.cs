using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }
    
    // 優化：用 Dictionary 存儲場景內的 CP，避免 FindObjectsOfType
    private Dictionary<string, CheckpointSetter> _sceneCheckpoints = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterCheckpoint(CheckpointSetter cp) => _sceneCheckpoints[cp.id] = cp;

    public bool HasSavedCheckpoint() => !string.IsNullOrEmpty(DataManager.Instance.gameData.lastCheckpointId);

    public bool IsCheckpointActivated(string id) => DataManager.Instance.gameData.triggeredTutorialIds.Contains("CP_" + id);

    public bool SaveCheckpointFirstTime(string checkpointId, Transform spawn)
    {
        // 借用 triggeredTutorialIds 來存 CP 是否啟動過，或者你可以在 GameData 加個清單
        if (IsCheckpointActivated(checkpointId)) return false;

        var data = DataManager.Instance.gameData;
        data.lastSceneName = SceneManager.GetActiveScene().name;
        data.lastCheckpointId = checkpointId;
        data.fallbackPos = spawn.position;
        data.fallbackEuler = spawn.rotation.eulerAngles;
        
        data.triggeredTutorialIds.Add("CP_" + checkpointId);
        
        DataManager.Instance.SaveToDisk();
        Debug.Log($"[Checkpoint] {checkpointId} Saved.");
        return true;
    }

    public CheckpointSetter GetCheckpointById(string id)
    {
        _sceneCheckpoints.TryGetValue(id, out var cp);
        return cp;
    }
}