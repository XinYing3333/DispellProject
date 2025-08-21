// CheckpointManager.cs
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
        public Vector3 pos;
        public Vector3 euler; // 也可用 Quaternion（序列化較麻煩）
    }

    private const string PREFS_KEY = "LAST_CHECKPOINT";

    public SaveStruct? LastCheckpoint { get; private set; }

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 若想跨開遊戲也保留，用這行載回：
        LoadFromPrefsIfAny();
    }

    public void SetActiveCheckpoint(CheckpointData data, bool saveToPrefs = true)
    {
        LastCheckpoint = new SaveStruct
        {
            scene = data.SceneName,
            pos = data.Position,
            euler = data.Rotation.eulerAngles
        };

        if (saveToPrefs)
        {
            var json = JsonUtility.ToJson(LastCheckpoint.Value);
            PlayerPrefs.SetString(PREFS_KEY, json);
            PlayerPrefs.Save();
        }
    }

    public bool HasCheckpointForCurrentScene()
    {
        if (LastCheckpoint is null) return false;
        string current = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return LastCheckpoint?.scene == current;
    }

    public bool TryGetRespawn(out Vector3 pos, out Quaternion rot)
    {
        pos = default; rot = default;
        if (LastCheckpoint is null) return false;

        var s = LastCheckpoint.Value;
        string current = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (s.scene != current) return false;

        pos = s.pos;
        rot = Quaternion.Euler(s.euler);
        return true;
    }

    public void ClearCheckpoint(bool alsoPrefs = false)
    {
        LastCheckpoint = null;
        if (alsoPrefs)
        {
            PlayerPrefs.DeleteKey(PREFS_KEY);
        }
    }

    private void LoadFromPrefsIfAny()
    {
        if (!PlayerPrefs.HasKey(PREFS_KEY)) return;
        try
        {
            var json = PlayerPrefs.GetString(PREFS_KEY);
            var s = JsonUtility.FromJson<SaveStruct>(json);
            LastCheckpoint = s;
        }
        catch { LastCheckpoint = null; }
    }
}

public readonly struct CheckpointData
{
    public readonly string SceneName;
    public readonly Vector3 Position;
    public readonly Quaternion Rotation;

    public CheckpointData(string sceneName, Vector3 position, Quaternion rotation)
    {
        SceneName = sceneName; Position = position; Rotation = rotation;
    }
}
