using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ChildBoundsReturner : MonoBehaviour
{
    [Header("範圍 (以這個物件的位置為中心)")]
    [SerializeField] private Vector3 halfExtents = new Vector3(25f, 25f, 25f);

    [Header("傳送回來的安全偏移 (避免卡牆/穿地)")]
    [SerializeField] private Vector3 returnOffset = new Vector3(0f, 0.5f, 0f);

    [Header("是否連同 Rigidbody 一起重置速度")]
    [SerializeField] private bool resetRigidbodyVelocity = true;

    // 子物件初始狀態快照
    private struct Snapshot
    {
        public Transform tr;
        public Vector3 localPos;
        public Quaternion localRot;
        public Vector3 localScale;

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

    // 需要時可在 Inspector 右鍵執行：或在其他腳本呼叫 RebuildSnapshot()
    [ContextMenu("Rebuild Snapshot")]
    public void RebuildSnapshot()
    {
        _snapshots.Clear();

        // 包含 inactive 子物件
        var children = GetComponentsInChildren<Transform>(true);

        foreach (var tr in children)
        {
            if (tr == transform) continue; // 跳過自己

            var rb = tr.GetComponent<Rigidbody>();

            _snapshots.Add(new Snapshot
            {
                tr = tr,
                localPos = tr.localPosition,
                localRot = tr.localRotation,
                localScale = tr.localScale,
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
                _snapshots[i] = s; // struct 回寫
            }
        }
    }

    private void ReturnToSnapshot(ref Snapshot s)
    {
        // 強制回到初始相對父物件的狀態
        s.tr.localPosition = s.localPos;
        s.tr.localRotation = s.localRot;
        s.tr.localScale = s.localScale;

        // 加安全偏移：以世界座標往上推一點
        s.tr.position += returnOffset;

        if (s.rb)
        {
            // 先暫停物理，避免 Teleport 後被碰撞反彈
            s.rb.isKinematic = true;

            if (resetRigidbodyVelocity)
            {
                s.rb.linearVelocity = Vector3.zero;
                s.rb.angularVelocity = Vector3.zero;
            }

            // 還原原本設定
            s.rb.useGravity = s.rbUseGravity;
            s.rb.isKinematic = s.rbWasKinematic;

            // 讓物理系統知道位置已改變
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
