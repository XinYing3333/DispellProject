using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string PathFor(string slot) =>
        System.IO.Path.Combine(Application.persistentDataPath, $"save_{slot}.json");

    public static void Save(string slot, SaveData data)
    {
        var json = JsonUtility.ToJson(data);
        File.WriteAllText(PathFor(slot), json);
#if UNITY_EDITOR
        Debug.Log($"[SaveSystem] Saved: {PathFor(slot)}");
#endif
    }

    public static SaveData Load(string slot)
    {
        var path = PathFor(slot);
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonUtility.FromJson<SaveData>(json);
    }
}