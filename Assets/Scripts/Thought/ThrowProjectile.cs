using DefaultNamespace.Thought;
using Player.InteractionSystem;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class ThrowProjectile : MonoBehaviour
{
    public ThoughtPayloadSO payload;
    [SerializeField] private float hitThreshold = 0.5f; // 撞擊強度門檻
    [SerializeField] private bool autoDespawn = true;
    public float autoDespawnDelay = 0.02f;

    void Awake()
    {
        // 確保 Rigidbody 不受重力影響或根據需求設定
        if(TryGetComponent<Rigidbody>(out var rb))
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // 1. 強度檢核：避免微小擦撞
        if (collision.relativeVelocity.magnitude < hitThreshold) return;

        // 2. 尋找接收介面
        IHitReceiver receiver = collision.gameObject.GetComponentInParent<IHitReceiver>();
        
        if (receiver != null)
        {
            ExecuteHit(receiver);
        }
    }

    private void ExecuteHit(IHitReceiver receiver)
    {
        receiver.OnHit(payload);
        if (autoDespawn)
        {
            Destroy(gameObject, autoDespawnDelay);
        }
    }
}