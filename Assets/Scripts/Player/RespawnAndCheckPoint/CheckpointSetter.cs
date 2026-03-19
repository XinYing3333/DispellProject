using DefaultNamespace.EventBus.Events.Core;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointSetter : MonoBehaviour
{
    [Header("ID（必填、唯一）")]
    public string id;

    [Header("Optional override spawn transform")]
    public Transform spawnPointOverride;

    [Header("Visual")]
    public bool showGizmo = true;
    public Color gizmoColor = new(0.2f, 0.8f, 1f, 0.4f);

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        if (string.IsNullOrEmpty(id)) id = gameObject.name;
    }

    public Transform GetSpawnTransform() => spawnPointOverride ? spawnPointOverride : transform;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var t = GetSpawnTransform();
        bool firstSaved = CheckpointManager.Instance.SaveCheckpointFirstTime(id, t);

        if (firstSaved)
        {
            // 一些一次性演出（音效/特效/提示）
            EventBus<OnCheckpointUpdated>.Raise(new OnCheckpointUpdated());
            CollectionSystem.SaveCollection();
            // 可加 animator.SetTrigger("Activated"); 等等
        }
        // 已觸發過就什麼都不做（不覆寫最近進度）
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;
        Gizmos.color = gizmoColor;
        var t = GetSpawnTransform();
        Gizmos.DrawSphere(t.position + Vector3.up * 0.2f, 0.25f);
        Gizmos.DrawRay(t.position, t.forward * 0.7f);
    }
}