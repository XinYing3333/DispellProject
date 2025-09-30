// Collectible.cs —— 可被吸收/撿取
using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Collider))]
public class Collectible : MonoBehaviour
{
    [Tooltip("收集系統用的ID/類型")]
    public string id;

    // 若你已有現成收集流程，就在外部呼叫它
    public void OnCollected()
    {
        // TODO: 呼叫你原本的 CollectionSystem / VFX / SFX
        // Example: CollectionSystem.CollectItem(...);
        // Destroy(gameObject); // 如果需要
    }
}