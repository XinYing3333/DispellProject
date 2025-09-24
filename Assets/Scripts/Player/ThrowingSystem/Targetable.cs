// Targetable.cs —— 可被瞄準/射擊
using UnityEngine;

[DisallowMultipleComponent]
public class Targetable : MonoBehaviour
{
    [Tooltip("瞄準點用的主碰撞器；不填則綜合所有Collider的Bounds中心")]
    public Collider mainCollider;

    [Tooltip("可選：高亮控制（若你已有 Highlightable 就拖進來）")]
    public Highlightable highlightable;

    public Vector3 GetAimPoint()
    {
        if (mainCollider) return mainCollider.bounds.center;
        var cols = GetComponentsInChildren<Collider>();
        if (cols.Length == 0) return transform.position;
        Bounds b = cols[0].bounds;
        for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
        return b.center;
    }

   public void SetHighlighted(bool on)
    {
        if (highlightable) highlightable.SetHighlighted(on);
    }
}