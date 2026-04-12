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
    }

    // 掉崖/陷阱：先扣血 → 回最近安全點；若無 → 回最近 checkpoint → 再無 → 用外部提供的 default（場景邏輯決定）
    public void RespawnAtLastSafe(Vector3 fallbackPos, Quaternion fallbackRot)
    {
        Time.timeScale = 1.0f;
        if (playerMovement && playerMovement.TryGetLastSafeGround(out var pos, out var rot))
        {
            // 在這裡加入檢查：LastSafeGround 是否落在當前允許的 groundMask 上
            if (Physics.CheckSphere(pos + Vector3.up * 0.1f, 0.2f, groundMask))
            {
                PlaceAt(pos, rot);
                return;
            }
        }

        // 回 checkpoint
        var data = DataManager.Instance.gameData; // 直接從數據中心拿
        if (!string.IsNullOrEmpty(data.lastCheckpointId) && 
            data.lastSceneName == UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
        {
            // 優先找場景內的實體 CheckpointSetter
            if (TryFindCheckpointSpawn(data.lastCheckpointId, out var cpos, out var crot))
            {
                PlaceAt(cpos, crot);
            }
            else
            {
                // 如果場景剛載入還沒找到實體，則使用備援座標
                PlaceAt(data.fallbackPos, Quaternion.Euler(data.fallbackEuler));
            }
        }

        // 最後用傳入的 fallback（通常是場景 defaultSpawnPoint）
        PlaceAt(fallbackPos, fallbackRot);
    }

    public void PlaceAt(Vector3 pos, Quaternion rot)
    {
        Time.timeScale = 1.0f;
        // 1. 執行現有的安全修正
        if (SafeSpawnUtility.EnsureSafeSpawn(pos, rot, out var safePos, out var safeRot, groundMask, 6f, 45f, 0.35f, 0.7f, 2f, 12))
        {
            pos = safePos; rot = safeRot;
        }

        // 2. 核心邏輯：驗證腳下地板的有效性
        // 從預定點稍高處向下偵測
        if (Physics.Raycast(pos + Vector3.up * 1f, Vector3.down, out RaycastHit hit, 2f, groundMask))
        {
            // 只有射線打到 groundMask 內的 Layer 才執行位移
            ExecuteTeleport(pos, rot);
        }
        else
        {
            // 若偵測失敗（例如該處已被排除在 groundMask 外），強制回傳到 fallback
            // 注意：這可能導致無窮遞迴，需謹慎處理
            Debug.LogWarning("無效的重生點 Layer，取消此次定位");
        }
    }

    private void ExecuteTeleport(Vector3 pos, Quaternion rot)
    {
        if (_rb)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
        transform.SetPositionAndRotation(pos, rot);
    }

    // 在 RespawnController 內部修改 TryFindCheckpointSpawn 方法
    private bool TryFindCheckpointSpawn(string checkpointId, out Vector3 pos, out Quaternion rot)
    {
        pos = default; rot = default;
        var cp = CheckpointManager.Instance.GetCheckpointById(checkpointId);
        if (cp != null)
        {
            var t = cp.GetSpawnTransform();
            pos = t.position; rot = t.rotation;
            return true;
        }
        return false;
    }
}
