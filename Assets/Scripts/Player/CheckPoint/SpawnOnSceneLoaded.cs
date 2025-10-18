using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnOnSceneLoaded : MonoBehaviour
{
    [Header("Fallback spawn if no checkpoint")]
    public Transform defaultSpawnPoint;

    private void OnEnable()  { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(CoPlacePlayer());
    }

    IEnumerator CoPlacePlayer()
    {
        yield return null; // 讓場景物件都就緒

        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) yield break;

        var respawn = player.GetComponent<RespawnController>();
        if (!respawn) yield break;

        // 入場只查 checkpoint
        var sceneName = SceneManager.GetActiveScene().name;
        if (CheckpointManager.Instance && CheckpointManager.Instance.HasCheckpointForCurrentScene())
        {
            respawn.RespawnAtCheckpoint();
        }
        else if (defaultSpawnPoint)
        {
            // 沒有 checkpoint → 用場景預設
            player.transform.SetPositionAndRotation(defaultSpawnPoint.position, defaultSpawnPoint.rotation);
        }
    }
}