// Checkpoint.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Header("Optional override spawn transform (leave null = use this transform)")]
    public Transform spawnPointOverride;

    [Header("Visual")]
    public bool showGizmo = true;
    public Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.4f);

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // 設成 Trigger
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // 取得重生座標與朝向
        Transform t = spawnPointOverride ? spawnPointOverride : transform;
        var data = new CheckpointData(
            sceneName: UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            position: t.position,
            rotation: t.rotation
        );

        CheckpointManager.Instance.SetActiveCheckpoint(data);
        // 你可以在這裡做個視覺提示（亮燈、音效）
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;
        Gizmos.color = gizmoColor;
        var t = spawnPointOverride ? spawnPointOverride : transform;
        Gizmos.DrawSphere(t.position + Vector3.up * 0.2f, 0.25f);
        Gizmos.DrawRay(t.position, t.forward * 0.7f);
    }
}