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
        if (string.IsNullOrEmpty(id)) id = gameObject.name; // 最起碼給個預設
    }

    public Transform GetSpawnTransform()
    {
        return spawnPointOverride ? spawnPointOverride : transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var t = GetSpawnTransform();
        CheckpointManager.Instance.SetActiveCheckpoint(id, t);

        // TODO: 視覺/音效反饋
        // e.g., animator.SetTrigger("Activated"); audioSource.PlayOneShot(sfx);
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