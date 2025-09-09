// Targetable.cs
using UnityEngine;

[DisallowMultipleComponent]
public class Targetable : MonoBehaviour, IShootable
{
    [Tooltip("用來計算瞄準點的碰撞器；不指定則用所有Collider的Bounds")]
    public Collider mainCollider;

    [Tooltip("可選：與 Highlightable 串接；不填就不高亮")]
    public Highlightable highlightable;

    public Vector3 GetAimPoint()
    {
        if (mainCollider) return mainCollider.bounds.center;

        var colls = GetComponentsInChildren<Collider>();
        if (colls.Length == 0) return transform.position;

        Bounds b = colls[0].bounds;
        for (int i = 1; i < colls.Length; i++) b.Encapsulate(colls[i].bounds);
        return b.center;
    }

    public void SetHighlighted(bool on)
    {
        if (highlightable) highlightable.SetHighlighted(on);
    }
}