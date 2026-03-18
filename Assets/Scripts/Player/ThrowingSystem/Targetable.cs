using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class Targetable : MonoBehaviour
{
    [Header("Aim Point")]
    [Tooltip("明確指定瞄準點（建議放在胸口/模型中心）")]
    public Transform aimAnchor;

    [Tooltip("主碰撞器（不建議用腳底或地面接觸 collider）")]
    public Collider mainCollider;

    [Tooltip("使用 Renderer bounds 作為中心，避免被腳底 collider 拉低")]
    public bool preferRendererBounds = true;

    [Tooltip("Bounds 高度位置：0=底部，0.5=中心，1=頂部")]
    [Range(0f, 1f)] public float aimHeight01 = 0.5f;

    [Header("描邊 / 高亮")]
    [SerializeField] private Outline outlineScript;
    [SerializeField] private GameObject outlineUICanvas;

    private bool _aimActive;

    private void Awake()
    {
        SetHighLightEnabled(false);
    }

    public Vector3 GetAimPoint()
    {
        // 1️⃣ 明確錨點（最優先、最穩）
        if (aimAnchor)
            return aimAnchor.position;

        // 2️⃣ Renderer bounds（視覺中心）
        if (preferRendererBounds && TryGetRendererBounds(out var rb))
            return PointByHeight(rb, aimHeight01);

        // 3️⃣ 主 Collider（你手動指定的）
        if (mainCollider)
            return PointByHeight(mainCollider.bounds, aimHeight01);

        // 4️⃣ 非 Trigger Collider bounds（排除腳底 trigger）
        if (TryGetNonTriggerColliderBounds(out var cb))
            return PointByHeight(cb, aimHeight01);

        // 5️⃣ 兜底
        return transform.position;
    }

    static Vector3 PointByHeight(Bounds b, float h01)
    {
        var p = b.center;
        p.y = Mathf.Lerp(b.min.y, b.max.y, h01);
        return p;
    }

    bool TryGetRendererBounds(out Bounds b)
    {
        var rends = GetComponentsInChildren<Renderer>(true);
        b = default;
        bool has = false;

        foreach (var r in rends)
        {
            if (!r || !r.enabled) continue;
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }
        return has;
    }

    bool TryGetNonTriggerColliderBounds(out Bounds b)
    {
        var cols = GetComponentsInChildren<Collider>(true);
        b = default;
        bool has = false;

        foreach (var c in cols)
        {
            if (!c || c.isTrigger) continue;
            if (!has) { b = c.bounds; has = true; }
            else b.Encapsulate(c.bounds);
        }
        return has;
    }

    public void SetAimActive(bool on)
    {
        if (_aimActive == on) return;
        _aimActive = on;
        SetHighLightEnabled(on);
    }

    private void SetHighLightEnabled(bool on)
    {
        if (outlineScript) outlineScript.enabled = on;
        if (outlineUICanvas) outlineUICanvas.SetActive(on);
    }

    private void OnDisable()
    {
        SetHighLightEnabled(false);
    }
}
