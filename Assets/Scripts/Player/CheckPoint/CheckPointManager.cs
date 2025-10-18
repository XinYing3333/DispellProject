using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Serializable]
    public struct SaveStruct
    {
        public string scene;
        public string checkpointId;   // ⭐ 用ID更穩定
        public Vector3 fallbackPos;   // 可選：當找不到ID時退而求其次
        public Vector3 fallbackEuler;
    }

    private const string PREFS_KEY = "LAST_CHECKPOINT_V2";

    // 每個場景一筆（如果你有多場景串接）
    private readonly Dictionary<string, SaveStruct> _byScene = new();

    public bool TryGetCheckpointSpawn(string scene, out Vector3 pos, out Quaternion rot)
    {
        pos = default; rot = default;
        if (!_byScene.TryGetValue(scene, out var s)) return false;

        // 依 ID 尋找場景中的 Checkpoint
        var cps = GameObject.FindObjectsOfType<CheckpointSetter>(true);
        foreach (var cp in cps)
        {
            if (cp.id == s.checkpointId)
            {
                var t = cp.GetSpawnTransform();
                pos = t.position; rot = t.rotation;
                return true;
            }
        }

        // 找不到相符ID → 用 fallback（避免地圖改動時完全無法重生）
        pos = s.fallbackPos;
        rot = Quaternion.Euler(s.fallbackEuler);
        return true;
    }

    public void SetActiveCheckpoint(string checkpointId, Transform spawn, bool saveToPrefs = true)
    {
        string scene = SceneManager.GetActiveScene().name;
        var data = new SaveStruct
        {
            scene = scene,
            checkpointId = checkpointId,
            fallbackPos = spawn.position,
            fallbackEuler = spawn.rotation.eulerAngles
        };
        _byScene[scene] = data;

        if (saveToPrefs)
        {
            var json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(PREFS_KEY, json);
            PlayerPrefs.Save();
        }
        Debug.Log($"Saved checkpoint {checkpointId}");
    }

    public bool HasCheckpointForCurrentScene()
    {
        return _byScene.ContainsKey(SceneManager.GetActiveScene().name);
    }

    public void ClearCheckpoint(bool alsoPrefs = false)
    {
        _byScene.Clear();
        if (alsoPrefs) PlayerPrefs.DeleteKey(PREFS_KEY);
    }

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFromPrefsIfAny();
    }

    private void LoadFromPrefsIfAny()
    {
        if (!PlayerPrefs.HasKey(PREFS_KEY)) return;
        try
        {
            var s = JsonUtility.FromJson<SaveStruct>(PlayerPrefs.GetString(PREFS_KEY));
            _byScene[s.scene] = s;
        }
        catch { /* ignore */ }
    }
}
