using UnityEngine;
using Player.InteractionSystem;
using UnityEngine.Serialization;

namespace DefaultNamespace.Thought
{
    [RequireComponent(typeof(Collider))]
    public class ThrowProjectile : MonoBehaviour
    {
        public ThoughtPayloadSO payload; // 這次丟的是哪種念頭
        [SerializeField]private bool autoDespawn = false;
        public float autoDespawnDelay = 0.02f; // 命中後延遲銷毀

        void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true; // 超簡版：用 Trigger 判定
        }

        void OnCollisionEnter(Collision collision)
        {
            // 嘗試往自己或父物件找 IHitReceiver
            if (!collision.gameObject.TryGetComponent<IHitReceiver>(out var receiver))
                receiver = collision.gameObject.GetComponentInParent<IHitReceiver>();

            if (receiver == null) return;

            receiver.OnHit(payload);
            if(!autoDespawn)return;
            Destroy(gameObject, autoDespawnDelay); // 命中後就回收（之後可換物件池）
        }
    }
}