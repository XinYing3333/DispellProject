using DefaultNamespace.EventBus.Events.Core;
using UnityEngine;

public class CheckpointSetter : MonoBehaviour
{
    public string id;
    public Transform spawnPointOverride;
    [SerializeField]private bool showGizmo;

    private void Start()
    {
        // 主動向管理員報到
        CheckpointManager.Instance.RegisterCheckpoint(this);
    }

    public Transform GetSpawnTransform() => spawnPointOverride ? spawnPointOverride : transform;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        if (CheckpointManager.Instance.SaveCheckpointFirstTime(id, GetSpawnTransform()))
        {
            EventBus<OnCheckpointUpdated>.Raise(new OnCheckpointUpdated());
            CollectionSystem.SaveCollection();
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;
        Gizmos.color = Color.yellow;
        var t = GetSpawnTransform();
        Gizmos.DrawSphere(t.position + Vector3.up * 0.2f, 0.25f);
        Gizmos.DrawRay(t.position, t.forward * 0.7f);
    }
}