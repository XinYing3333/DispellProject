using UnityEngine;

namespace Player.InteractionSystem
{
    public class HandSlot : MonoBehaviour
    {
        [SerializeField] private Transform anchor; // 掛點（放在角色手邊的空物件）
        public bool HasItem => _held != null;
        public Rigidbody HeldRigidbody => _held;

        private Rigidbody _held;
        private IMagnetAttachable _heldMagnet;

        public bool TryAttach(Rigidbody rb)
        {
            if (!rb || HasItem) return false;

            _held = rb;
            _heldMagnet = rb.GetComponentInParent<IMagnetAttachable>();
            _heldMagnet?.OnMagnetAttached(anchor);

            rb.transform.SetParent(anchor, false);
            rb.transform.localPosition = Vector3.zero;
            rb.transform.localRotation = Quaternion.identity;

            // 核心最小版：手上時凍結基本運動，避免亂晃
            var body = rb;
            body.isKinematic = true;
            body.useGravity = false;
            body.detectCollisions = false;

            return true;
        }

        public Rigidbody Take() // 拋之前取出
        {
            var r = _held;
            if (r) r.transform.SetParent(null, true);
            _held = null;
            _heldMagnet = null;
            return r;
        }

        public void Detach() // 丟地上（不拋）
        {
            if (!_held) return;
            _heldMagnet?.OnMagnetDetached();
            _held.transform.SetParent(null, true);
            var rb = _held;

            _held = null;
            _heldMagnet = null;

            // 還原最基本物理
            if (rb)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.detectCollisions = true;
            }
        }
        
        // ---- 放在 HandSlot 類別結尾 ----
        private void OnDrawGizmosSelected()
        {
            if (!anchor) return;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(anchor.position, 0.08f);
            Gizmos.DrawRay(anchor.position, anchor.forward * 0.25f);
        }

    }
}