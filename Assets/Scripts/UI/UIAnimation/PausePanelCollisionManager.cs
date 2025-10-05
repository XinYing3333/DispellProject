using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class PausePanelCollisionAnimator : MonoBehaviour
{
    [Header("面板內元素間距")]
    public float verticalSpacing = 10f;
    public float horizontalSpacing = 10f;

    [Header("碰撞推擠強度")]
    public float pushStrength = 5f;

    [Header("平滑碰撞動畫")]
    public float smoothTime = 0.1f;

    [Header("自動更新")]
    public bool autoUpdate = true;

    private RectTransform panelRect;
    private List<RectTransform> children = new List<RectTransform>();
    private Dictionary<RectTransform, Vector3> velocityMap = new Dictionary<RectTransform, Vector3>();

    void Awake()
    {
        panelRect = GetComponent<RectTransform>();
        CollectChildren();
    }

    void Update()
    {
        if (!Application.isPlaying && autoUpdate)
        {
            CollectChildren();
            ResolveCollisions();
        }
    }

    public void CollectChildren()
    {
        children.Clear();
        velocityMap.Clear();
        foreach (Transform t in transform)
        {
            RectTransform rt = t as RectTransform;
            if (rt != null && t.gameObject.activeSelf)
            {
                children.Add(rt);
                velocityMap[rt] = Vector3.zero;
            }
        }
    }

    public void ResolveCollisions()
    {
        if (children.Count == 0) return;

        for (int i = 0; i < children.Count; i++)
        {
            RectTransform a = children[i];
            Rect aRect = GetWorldRect(a);

            for (int j = i + 1; j < children.Count; j++)
            {
                RectTransform b = children[j];
                Rect bRect = GetWorldRect(b);

                if (aRect.Overlaps(bRect))
                {
                    Vector3 dir = (b.position - a.position).normalized;
                    if (dir == Vector3.zero) dir = Vector3.up; // 避免完全重疊

                    Vector3 push = dir * pushStrength;

                    // 平滑過渡
                    Vector3 pushA = -push * 0.5f;
                    Vector3 pushB = push * 0.5f;

                    // 取出暫存速度
                    Vector3 velA = velocityMap[a];
                    Vector3 velB = velocityMap[b];

                    // 更新位置
                    a.localPosition = Vector3.SmoothDamp(a.localPosition, a.localPosition + pushA, ref velA, smoothTime);
                    b.localPosition = Vector3.SmoothDamp(b.localPosition, b.localPosition + pushB, ref velB, smoothTime);

                    // 寫回 Dictionary
                    velocityMap[a] = velA;
                    velocityMap[b] = velB;
                }
            }
        }

        CenterChildren();
    }

    private void CenterChildren()
    {
        if (children.Count == 0) return;

        Vector3 avgPos = Vector3.zero;
        foreach (var child in children)
        {
            avgPos += child.localPosition;
        }
        avgPos /= children.Count;

        foreach (var child in children)
        {
            child.localPosition -= avgPos;
        }
    }

    private Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector3 bottomLeft = corners[0];
        Vector3 topRight = corners[2];
        return new Rect(bottomLeft.x, bottomLeft.y, topRight.x - bottomLeft.x, topRight.y - bottomLeft.y);
    }

    // 在 DOTween 動畫中可以呼叫這個方法
    public void UpdateCollisionsDuringTween()
    {
        ResolveCollisions();
    }
}
