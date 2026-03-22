using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ChildBoundsReturner : MonoBehaviour
{
    [Header("範圍 (以這個物件的局部座標為準)")]
    [SerializeField] private Vector3 halfExtents = new Vector3(25f, 25f, 25f);

    [Header("傳送回來的安全偏移 (世界座標)")]
    [SerializeField] private Vector3 returnOffset = new Vector3(0f, 0.5f, 0f);

    [Header("是否連同 Rigidbody 一起重置速度")]
    [SerializeField] private bool resetRigidbodyVelocity = true;

    [Header("越界時是否把物件掛回原本父物件")]
    [SerializeField] private bool reparentOnReturn = true;

    private struct Snapshot
    {
        public Transform tr;
        public Vector3 worldPos;
        public Quaternion worldRot;
        public Transform originalParent;
        public Rigidbody rb;
        public bool rbWasKinematic;
        public bool rbUseGravity;
        public bool valid;
    }

    private readonly List<Snapshot> _snapshots = new();

    private void Awake() => RebuildSnapshot();

    [ContextMenu("Rebuild Snapshot")]
    public void RebuildSnapshot()
    {
        _snapshots.Clear();
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
        for (int i = 0; i < _snapshots.Count; i++)
        {
            var s = _snapshots[i];
            if (!s.valid || !s.tr) continue;

            // 關鍵修改：將世界座標轉為此物件的局部座標
            Vector3 localPos = transform.InverseTransformPoint(s.tr.position);

            // 在局部空間判斷是否超出 halfExtents
            if (Mathf.Abs(localPos.x) > halfExtents.x || 
                Mathf.Abs(localPos.y) > halfExtents.y || 
                Mathf.Abs(localPos.z) > halfExtents.z)
            {
                ReturnToSnapshot(ref s);
                _snapshots[i] = s;
            }
        }
    }

    private void ReturnToSnapshot(ref Snapshot s)
    {
        if (!s.tr) return;

        if (s.rb) s.rb.isKinematic = true;

        if (reparentOnReturn) s.tr.SetParent(s.originalParent, true);

        s.tr.SetPositionAndRotation(s.worldPos + returnOffset, s.worldRot);

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
            s.rb.useGravity = s.rbUseGravity;
            s.rb.isKinematic = s.rbWasKinematic;
            s.rb.WakeUp();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 關鍵修改：將 Gizmos 矩陣設為此物件的變換矩陣，使其隨旋轉連動
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
    }
#endif
}