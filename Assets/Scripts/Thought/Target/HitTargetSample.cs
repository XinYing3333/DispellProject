using DefaultNamespace.Thought;
using Player.InteractionSystem;
using UnityEngine;
using UnityEngine.Events;

namespace Thought
{
    public class HitTargetSample : MonoBehaviour, IHitReceiver
    {
        public ThoughtPayloadSO requiredPayload; // 空=任何念頭都可
        //public UnityEvent onHit;

        public void OnHit(ThoughtPayloadSO payload)
        {
            if (requiredPayload && payload != requiredPayload) return;
            SetEvent(payload);
        }

        private void SetEvent(ThoughtPayloadSO payload)
        {
            Debug.Log(payload.id);
        }
    }
}