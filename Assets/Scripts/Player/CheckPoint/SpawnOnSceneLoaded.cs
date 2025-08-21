// SpawnOnSceneLoaded.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnOnSceneLoaded : MonoBehaviour
{
    [Header("Fallback spawn if no checkpoint")]
    public Transform defaultSpawnPoint; // 場景預設出生點

    [Header("Ground snapping")]
    public bool snapToGround = true;
    public float groundCheckDown = 5f;
    public LayerMask groundMask = ~0;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(CoPlacePlayer());
    }

    IEnumerator CoPlacePlayer()
    {
        // 等一幀讓場景物件初始化
        yield return null;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) yield break;

        Vector3 pos; Quaternion rot;

        if (CheckpointManager.Instance && CheckpointManager.Instance.TryGetRespawn(out pos, out rot))
        {
            Place(player, pos, rot);
        }
        else if (defaultSpawnPoint)
        {
            Place(player, defaultSpawnPoint.position, defaultSpawnPoint.rotation);
        }
    }

    void Place(GameObject player, Vector3 pos, Quaternion rot)
    {
        if (snapToGround && Physics.Raycast(pos + Vector3.up, Vector3.down, out var hit, groundCheckDown + 1f, groundMask))
        {
            pos = hit.point;
        }

        // 關掉控制器再設置位置，避免自動修正

        var rb = player.GetComponent<Rigidbody>();
        //if (rb) { rb.isKinematic = true; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

        player.transform.SetPositionAndRotation(pos, rot);

        //if (rb) rb.isKinematic = false;
    }
}