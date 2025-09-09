// Highlightable.cs
using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class Highlightable : MonoBehaviour
{
    [Header("方案 A：子物件外框")]
    [Tooltip("把外框模型(只描邊/單色)放成子物件，拖進來；平常關閉，用來高亮")]
    public GameObject outlineChild;

    [Header("方案 B：材質切換（所有 Renderer ）")]
    public bool useMaterialSwitch = false;
    public Material outlineMaterial;

    private readonly List<Renderer> _renderers = new();
    private readonly List<Material[]> _originalMats = new();

    private bool _initialized;

    void Awake()
    {
        _renderers.AddRange(GetComponentsInChildren<Renderer>(true));
        foreach (var r in _renderers)
            _originalMats.Add(r.sharedMaterials);
        _initialized = true;

        if (outlineChild) outlineChild.SetActive(false);
    }

    public void SetHighlighted(bool on)
    {
        if (!_initialized) Awake();

        // 方案 A
        if (outlineChild) outlineChild.SetActive(on);

        // 方案 B
        if (useMaterialSwitch && outlineMaterial)
        {
            for (int i = 0; i < _renderers.Count; i++)
            {
                var r = _renderers[i];
                if (on)
                {
                    // 疊一層 outline 材質（放在最後一層，通常用 ZTest Always/描邊）
                    var mats = new List<Material>(_originalMats[i]);
                    if (!mats.Contains(outlineMaterial))
                    {
                        mats.Add(outlineMaterial);
                        r.sharedMaterials = mats.ToArray();
                    }
                }
                else
                {
                    r.sharedMaterials = _originalMats[i];
                }
            }
        }
    }
}