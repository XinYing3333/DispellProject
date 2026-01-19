using System.Collections.Generic;
using UnityEngine;

namespace Player.InteractionSystem
{
    public class HandSlot : MonoBehaviour
    {
        [Header("Anchor")]
        [SerializeField] private Transform anchor; // 放在角色手邊/手骨下的空物件

        [Header("Align Settings")]
        [SerializeField, Tooltip("吸附時是否讓物件正面朝向玩家")] 
        private bool alignRotationToForward = true;
        [SerializeField, Tooltip("合併包圍盒時是否忽略 Trigger Colliders")] 
        private bool ignoreTriggerColliders = true;
        [SerializeField, Tooltip("距離 anchor 再留一點空隙，避免極小重疊")] 
        private float holdPadding = 0.05f;

        [Header("Physics While Holding")]
        [SerializeField, Tooltip("拿在手上時把剛體設為 isKinematic、關閉碰撞（保留你原本表現）")]
        private bool kinematicWhileHolding = true;

        [Header("Debug/Workaround")]
        [SerializeField, Tooltip("若外部系統每幀覆寫 anchor.worldPosition，可暫時打勾：拿著物品時強制維持 anchor.localPosition")]
        private bool stabilizeAnchorWhileHolding = false;

        public bool HasItem => _held != null;
        public Rigidbody HeldRigidbody => _held;

        private Rigidbody _held;
        private IMagnetAttachable _heldMagnet;

        // ——— Workaround: 錨點 localPosition 鎖定（可選）
        private Vector3 _savedAnchorLocalPos;
        private bool _lockAnchor;

        public bool TryAttach(Rigidbody rb)
        {
            if (!rb || HasItem) return false;
            if (!anchor) anchor = transform;

            _held = rb;

            // 1) 先做「瞬間」朝向對齊（不要用 MoveRotation）
            if (alignRotationToForward)
            {
                var face = -anchor.forward;
                rb.transform.rotation = Quaternion.LookRotation(face, Vector3.up);
                Physics.SyncTransforms(); // ★ 立刻讓 collider/bounds 更新
            }

            // 2) 基於（已更新）的 collider.bounds 計算貼手位置
            //    如果很多物件只有 trigger，且你想用它算體積，這裡可選擇改成 false
            var targetPos = ComputeHoldCenter(rb.transform, anchor, holdPadding, ignoreTriggerColliders);
            rb.transform.position = targetPos;
            Physics.SyncTransforms(); // ★ 再次同步，確保親子關係前姿態正確

            // 3) 親子掛接（維持世界姿態）；這步不會改變世界位置/旋轉
            rb.transform.SetParent(anchor, true);

            // 4) 手上物物理表現（維持你原本效果）
            if (kinematicWhileHolding)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.detectCollisions = false;
            }

            // 5) 通知（不在這裡動 anchor）
            _heldMagnet = rb.GetComponentInParent<IMagnetAttachable>();
            _heldMagnet?.OnMagnetAttached(anchor);

            // 6) （可選）錨點防護
            if (stabilizeAnchorWhileHolding && anchor && anchor.parent)
            {
                _savedAnchorLocalPos = anchor.localPosition;
                _lockAnchor = true;
            }

            return true;
        }

        public Rigidbody Take() // 拋之前取出
        {
            var r = _held;
            if (r) r.transform.SetParent(null, true);

            _held = null;
            _heldMagnet = null;
            _lockAnchor = false;
            return r;
        }

        public void Detach() // 丟地上（不拋）
        {
            if (!_held) return;

            _heldMagnet?.OnMagnetDetached();

            var rb = _held;
            rb.transform.SetParent(null, true);

            _held = null;
            _heldMagnet = null;
            _lockAnchor = false;

            if (kinematicWhileHolding)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.detectCollisions = true;
            }
        }

        // —— Align by collider bounds on -anchor.forward
        private static Vector3 ComputeHoldCenter(Transform item, Transform anchor, float padding, bool ignoreTriggers)
        {
            var cols = item.GetComponentsInChildren<Collider>(true);
            Bounds merged = default; bool has = false;

            foreach (var c in cols)
            {
                if (!c) continue;
                if (ignoreTriggers && c.isTrigger) continue;
                if (!has) { merged = c.bounds; has = true; }
                else merged.Encapsulate(c.bounds);
            }

            if (!has) return anchor.position;

            Vector3 fwd = -anchor.forward;
            Vector3 af  = new(Mathf.Abs(fwd.x), Mathf.Abs(fwd.y), Mathf.Abs(fwd.z));
            float halfDepth = Vector3.Dot(merged.extents, af);

            Vector3 targetCenter = anchor.position + fwd * (halfDepth + padding);
            Vector3 delta = targetCenter - merged.center;
            return item.position + delta;
        }

        private void LateUpdate()
        {
            // 可選的防護：持有期間把 anchor 的 localPosition 維持成起始值
            if (_lockAnchor && HasItem && anchor && anchor.parent)
            {
                anchor.localPosition = _savedAnchorLocalPos;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!anchor) return;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(anchor.position, 0.08f);
            Gizmos.DrawRay(anchor.position, anchor.forward * 0.25f);
        }
    }
}
