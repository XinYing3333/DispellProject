using System;
using EventBus.Events.Health;
using Player;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private CharacterController _cc;
    
    void Awake()
    {
        if (playerMovement.TryGetLastSafeGround(out var pos, out var rot))
        {
            PlacePlayer(pos, rot);
        }
        else
        {
            RespawnAtCheckpoint();
        }

        _rb = GetComponent<Rigidbody>();
        _cc = GetComponent<CharacterController>();
    }
    
    // 掉崖、陷阱：先扣血，再回最近安全點；若無 → 回 checkpoint；再無 → 預設點
    public void RespawnAtLastSafe()
    {
        if (playerMovement && playerMovement.TryGetLastSafeGround(out var pos, out var rot))
        {
            PlacePlayer(pos, rot);
        }
        else
        {
            Debug.Log("Can't find Last Safe Ground");

            RespawnAtCheckpoint(); // 退而求其次
        }
    }

    // 關卡入口/讀檔/手動返回 checkpoint
    public void RespawnAtCheckpoint()
    {
        var scene = SceneManager.GetActiveScene().name;
        
        if (CheckpointManager.Instance && CheckpointManager.Instance.TryGetCheckpointSpawn(scene, out var pos, out var rot))
        {
            PlacePlayer(pos, rot);
        }
        else
        {
            // 找不到 → 嘗試場景內的 SpawnOnSceneLoaded 預設出生點
            Debug.Log("Can't find checkpoint spawn");
            var fallback = GameObject.FindObjectOfType<SpawnOnSceneLoaded>();
            if (fallback && fallback.defaultSpawnPoint)
                PlacePlayer(fallback.defaultSpawnPoint.position, fallback.defaultSpawnPoint.rotation);
        }
    }

    private void PlacePlayer(Vector3 pos, Quaternion rot)
    {
        DoPlace(pos, rot);
    }

    private void DoPlace(Vector3 pos, Quaternion rot)
    {
        // 在放置前修正重生位置
        if (SafeSpawnUtility.EnsureSafeSpawn(
                desiredPos: pos,
                desiredRot: rot,
                finalPos: out var safePos,
                finalRot: out var safeRot,
                groundMask: groundMask,     // 你已有的 LayerMask
                rayDown: 6f,
                slopeLimitDeg: 45f,         // 跟玩家可站立坡度一致
                probeRadius: 0.35f,
                edgeProbeDist: 0.7f,
                safeInset: 2f,
                radialChecks: 12))
        {
            pos = safePos;
            rot = safeRot;
        }

        // 暫時關掉控制器
        bool ccEnabled = false;
        if (_cc) { ccEnabled = _cc.enabled; _cc.enabled = false; }
        //if (_rb) { _rb.isKinematic = true; _rb.linearVelocity = Vector3.zero; _rb.angularVelocity = Vector3.zero; }
        
        transform.SetPositionAndRotation(pos, rot);

        //if (_rb) _rb.isKinematic = false;
        if (_cc) _cc.enabled = ccEnabled;
    }
}
