using System;
using DefaultNamespace.EventBus.Events.Dialog;
using DialogSystem;
using UnityEngine;
using Player.InteractionSystem;

namespace Player
{
    /// <summary>
    /// 場景互動點的互動脚本：對話通知、IFocusable互動。
    /// </summary>
    public class PlayerFocusController : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private Transform origin;
        [SerializeField] private float range = 1.2f;
        [SerializeField] private LayerMask mask;

        [Header("Gizmo Settings")]
        [SerializeField] private Color gizmoColor = new Color(1f, 0.6f, 0f, 0.35f);
        [SerializeField] private Color hitColor = Color.red;
        [SerializeField] private bool showSphereCast = true; // 只作視覺參考

        private IInteractable _current;
        private IInteractable _previous;

        private EventBinding<OnDialogueStarted> _bindingStart;
        private EventBinding<OnDialogueEnded> _bindingEnd;

        private bool _lockInteract = false;

        private void Awake()
        {
            if (!origin) origin = transform; // ★ 保護：沒指定就用自己

            // ★ 綁定事件 → 統一走 SetInteractLock，會自動清焦點
            _bindingStart = new EventBinding<OnDialogueStarted>(() => SetInteractLock(true));
            _bindingEnd   = new EventBinding<OnDialogueEnded>(() => SetInteractLock(false));
        }

        private void OnEnable()
        {
            if (_bindingStart != null) EventBus<OnDialogueStarted>.Register(_bindingStart);
            if (_bindingEnd   != null) EventBus<OnDialogueEnded>.Register(_bindingEnd);
        }

        private void OnDisable()
        {
            // ★ 安全解註冊（避免殘留引用）
            if (_bindingStart != null) EventBus<OnDialogueStarted>.Deregister(_bindingStart);
            if (_bindingEnd   != null) EventBus<OnDialogueEnded>.Deregister(_bindingEnd);
        }

        private void Update()
        {
            if (_lockInteract) return; // ★ 鎖定時不再處理（焦點已在 SetInteractLock(true) 收掉）

            _current = FindInteractable();

            // 焦點切換檢測（只在變更時呼叫）
            if (!ReferenceEquals(_current, _previous))
            {
                if (_previous is IFocusable oldF) oldF.OnFocusLost();
                if (_current  is IFocusable newF) newF.OnFocusGained();
                _previous = _current;
            }

            if (_current != null && PlayerInputHandler.Instance.InteractPressed)
                _current.Interact();
        }

        // —— 封裝「上鎖/解鎖」：上鎖時清掉當前焦點與提示 —— //
        public void SetInteractLock(bool locked)
        {
            if (_lockInteract == locked) return;
            _lockInteract = locked;

            if (_lockInteract)
                ClearFocus(); // ★ 切換到「鎖」時清一次，避免殘留提示/UI
        }

        private void ClearFocus()
        {
            if (_previous is IFocusable oldF) oldF.OnFocusLost();
            _previous = null;
            _current  = null;
        }

        private IInteractable FindInteractable()
        {
            Ray ray = new Ray(origin.position, origin.forward);
            if (Physics.Raycast(ray, out var hit, range, mask, QueryTriggerInteraction.Ignore))
                return hit.collider.GetComponentInParent<IInteractable>();
            return null;
        }

        // ---- Scene 可視化 ----
        private void OnDrawGizmos()
        {
            Transform o = origin ? origin : transform;

            // 射線（方向/長度）
            Gizmos.color = gizmoColor;
            Gizmos.DrawRay(o.position, o.forward * range);

            // 命中時在終點畫一顆球（僅遊玩時）
            if (Application.isPlaying && _current != null)
            {
                Gizmos.color = hitColor;
                Gizmos.DrawSphere(o.position + o.forward * range * 0.95f, 0.1f);
            }

            // 額外：終點位置的參考球
            if (showSphereCast)
            {
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireSphere(o.position + o.forward * range, 0.2f);
            }
        }
    }
}
