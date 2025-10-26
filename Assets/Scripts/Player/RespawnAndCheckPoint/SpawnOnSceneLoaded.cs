using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnOnSceneLoaded : MonoBehaviour
{
    [Header("Fallback spawn if no checkpoint")]
    public Transform defaultSpawnPoint;

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(CoPlacePlayer());
    }

    IEnumerator CoPlacePlayer()
    {
        yield return null; // 等場景物件就緒

        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) yield break;

        var respawn = player.GetComponent<RespawnController>();
        if (!respawn) yield break;

        // 1) 嘗試從 PlayerPrefs 還原
        if (CheckpointManager.Instance.TryLoadSavedCheckpoint(out var data) &&
            data.scene == SceneManager.GetActiveScene().name)
        {
            // 先找實際 checkpoint 位置
            if (TryFindCheckpointSpawn(data.checkpointId, out var pos, out var rot))
            {
                respawn.PlaceAt(pos, rot);
                yield break;
            }
            Debug.Log("spawn at fall back");
            // 找不到ID → 用 fallback
            respawn.PlaceAt(data.fallbackPos, Quaternion.Euler(data.fallbackEuler));
            yield break;
        }

        // 2) 沒有任何記錄 → 用場景預設出生點
        if (defaultSpawnPoint)
        {
            Debug.Log("spawn at Default spawn point");
            respawn.PlaceAt(defaultSpawnPoint.position, defaultSpawnPoint.rotation);
        }
    }

    private bool TryFindCheckpointSpawn(string checkpointId, out Vector3 pos, out Quaternion rot)
    {
        pos = default; rot = default;
        var cps = GameObject.FindObjectsOfType<CheckpointSetter>(true);
        foreach (var cp in cps)
        {
            if (cp.id == checkpointId)
            {
                var t = cp.GetSpawnTransform();
                pos = t.position; rot = t.rotation;
                return true;
            }
        }
        return false;
    }
}