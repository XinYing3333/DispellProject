using UnityEngine;
using UnityEngine.Serialization;

public enum TargetState
{
    None,
    ThrowableReady, // 手持拋擲物時
    SpellReady      // 一般法術感應時
}

[DisallowMultipleComponent]
public class Targetable : MonoBehaviour
{
    [Header("Aim Point")]
    public Transform aimAnchor;
    public Collider mainCollider;
    public bool preferRendererBounds = true;
    [Range(0f, 1f)] public float aimHeight01 = 0.5f;

    [Header("Visuals")]
    [SerializeField] private Outline outlineScript;
    [SerializeField] private GameObject outlineUICanvas;
    private Color _throwableColor = Color.yellow;
    private Color _spellColor = Color.red;

    private void Awake()
    {
        // 確保一開始是關閉的
        SetTargetState(TargetState.None);
    }

    public void SetTargetState(TargetState state)
    {
        bool isActive = state != TargetState.None;

        if (outlineScript)
        {
            outlineScript.enabled = isActive;
            if (isActive)
            {
                outlineScript.OutlineColor = (state == TargetState.ThrowableReady) 
                    ? _throwableColor 
                    : _spellColor;
            }
        }

        if (outlineUICanvas)
        {
            outlineUICanvas.SetActive(isActive);
        }
    }

    public Vector3 GetAimPoint()
    {
        if (aimAnchor) return aimAnchor.position;
        if (preferRendererBounds && TryGetRendererBounds(out var rb)) return PointByHeight(rb, aimHeight01);
        if (mainCollider) return PointByHeight(mainCollider.bounds, aimHeight01);
        return transform.position;
    }

    static Vector3 PointByHeight(Bounds b, float h01) 
    {
        Vector3 p = b.center;
        p.y = Mathf.Lerp(b.min.y, b.max.y, h01);
        return p;
    }

    bool TryGetRendererBounds(out Bounds b)
    {
        var rends = GetComponentsInChildren<Renderer>(true);
        b = new Bounds();
        if (rends.Length == 0) return false;
        b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        return true;
    }
}
