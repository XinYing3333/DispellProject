using Player;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class RespawnController : MonoBehaviour
{
    [Header("Refs")]
    public PlayerMovement playerMovement;

    [Header("Ground snapping on respawn")]
    public bool snapToGround = true;
    public float groundCheckDown = 5f;
    public LayerMask groundMask = ~0;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // ⚠️ 不在這裡移動玩家，入場初始放置交給 SpawnOnSceneLoaded
    }

    // 掉崖/陷阱：先扣血 → 回最近安全點；若無 → 回最近 checkpoint → 再無 → 用外部提供的 default（場景邏輯決定）
    public void RespawnAtLastSafe(Vector3 fallbackPos, Quaternion fallbackRot)
    {
        if (playerMovement && playerMovement.TryGetLastSafeGround(out var pos, out var rot))
        {
            PlaceAt(pos, rot);
            return;
        }

        // 回 checkpoint
        if (CheckpointManager.Instance.TryLoadSavedCheckpoint(out var data) &&
            data.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
        {
            if (TryFindCheckpointSpawn(data.checkpointId, out var cpos, out var crot))
            {
                PlaceAt(cpos, crot); 
                return;
            }
            PlaceAt(data.fallbackPos, Quaternion.Euler(data.fallbackEuler));
            return;
        }

        // 最後用傳入的 fallback（通常是場景 defaultSpawnPoint）
        PlaceAt(fallbackPos, fallbackRot);
    }

    public void PlaceAt(Vector3 pos, Quaternion rot)
    {
        // 位置安全修正
        if (SafeSpawnUtility.EnsureSafeSpawn(
            pos, rot, out var safePos, out var safeRot,
            groundMask, 6f, 45f, 0.35f, 0.7f, 2f, 12))
        {
            pos = safePos; rot = safeRot;
        }

        // 關掉剛體速度以避免落地亂彈
        if (_rb)
        {
            // _rb.isKinematic = true;
            _rb.linearVelocity  = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        transform.SetPositionAndRotation(pos, rot);
        Time.timeScale = 1.0f;
        // if (_rb) _rb.isKinematic = false;
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
