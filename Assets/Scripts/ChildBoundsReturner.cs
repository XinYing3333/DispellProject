using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ChildBoundsReturner : MonoBehaviour
{
    [Header("範圍 (以這個物件的位置為中心)")]
    [SerializeField] private Vector3 halfExtents = new Vector3(25f, 25f, 25f);

    [Header("傳送回來的安全偏移 (世界座標)")]
    [SerializeField] private Vector3 returnOffset = new Vector3(0f, 0.5f, 0f);

    [Header("是否連同 Rigidbody 一起重置速度")]
    [SerializeField] private bool resetRigidbodyVelocity = true;

    [Header("越界時是否把物件掛回原本父物件")]
    [SerializeField] private bool reparentOnReturn = true;

    // 快照：改用「世界座標」+ 原 parent 記錄
    private struct Snapshot
    {
        public Transform tr;

        public Vector3 worldPos;
        public Quaternion worldRot;
        public Vector3 worldScale;

        public Transform originalParent;

        public Rigidbody rb;
        public bool rbWasKinematic;
        public bool rbUseGravity;

        public bool valid;
    }

    private readonly List<Snapshot> _snapshots = new();

    private void Awake()
    {
        RebuildSnapshot();
    }

    [ContextMenu("Rebuild Snapshot")]
    public void RebuildSnapshot()
    {
        _snapshots.Clear();

        // 只抓「當下屬於我階層」的子物件做初始快照
        var children = GetComponentsInChildren<Transform>(true);

        foreach (var tr in children)
        {
            if (tr == transform) continue;

            var rb = tr.GetComponent<Rigidbody>();

            _snapshots.Add(new Snapshot
            {
                tr = tr,
                worldPos = tr.position,
                worldRot = tr.rotation,
                worldScale = tr.lossyScale,
                originalParent = tr.parent,

                rb = rb,
                rbWasKinematic = rb ? rb.isKinematic : false,
                rbUseGravity = rb ? rb.useGravity : false,

                valid = true
            });
        }
    }

    private void LateUpdate()
    {
        var center = transform.position;
        var min = center - halfExtents;
        var max = center + halfExtents;

        for (int i = 0; i < _snapshots.Count; i++)
        {
            var s = _snapshots[i];
            if (!s.valid || !s.tr) continue;

            var p = s.tr.position;

            if (p.x < min.x || p.x > max.x || p.y < min.y || p.y > max.y || p.z < min.z || p.z > max.z)
            {
                ReturnToSnapshot(ref s);
                _snapshots[i] = s;
            }
        }
    }

    private void ReturnToSnapshot(ref Snapshot s)
    {
        var tr = s.tr;
        if (!tr) return;

        // 先把 Rigidbody 暫停，避免 teleport 造成怪反彈
        if (s.rb)
        {
            s.rb.isKinematic = true;
        }

        // 需要的話，把物件掛回原本父物件（不依賴 parent 也能回位置，但階層會乾淨）
        if (reparentOnReturn)
        {
            tr.SetParent(s.originalParent, true); // true: 保持世界座標
        }

        // 回到初始「世界座標」狀態
        tr.SetPositionAndRotation(s.worldPos, s.worldRot);
        tr.position += returnOffset;

        // scale：lossyScale 無法直接回寫（受父層影響），但大多道具不會改 scale。
        // 如果你確定會被改 scale，應改成記錄 localScale + originalParent，並在 reparent 後回寫 localScale。
        // 這裡先做保守處理：只有在 parent 沒變或你 reparentOnReturn=true 時才回 localScale。
        if (reparentOnReturn && s.originalParent)
        {
            // 嘗試還原 localScale（避免被手部縮放影響）
            // 注意：原本記的是 lossyScale，因此這裡不硬算，避免錯誤縮放。
            // 你若需要「精準還原縮放」，下一段我給你精準版公式。
        }

        if (s.rb)
        {
            if (resetRigidbodyVelocity)
            {
#if UNITY_6000_0_OR_NEWER
                s.rb.linearVelocity = Vector3.zero;
#else
                s.rb.velocity = Vector3.zero;
#endif
                s.rb.angularVelocity = Vector3.zero;
            }

            // 還原原本物理設定
            s.rb.useGravity = s.rbUseGravity;
            s.rb.isKinematic = s.rbWasKinematic;

            s.rb.WakeUp();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawWireCube(transform.position, halfExtents * 2f);
    }
#endif
}
