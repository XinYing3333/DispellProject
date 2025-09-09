using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class Highlightable : MonoBehaviour
{
    [Header("通用外框（兩路共用）")]
    public GameObject outlineChild;

    [Header("材質疊加（可不用）")]
    public bool useMaterialSwitch = false;
    public Material aimOutlineMat;       // 瞄準時材質（可為同一張）
    public Material proximityOutlineMat; // 近距離時材質

    private readonly List<Renderer> _renderers = new();
    private readonly List<Material[]> _originalMats = new();

    // 兩路「開關」；任一路 true 就是亮著
    private bool _aimOn, _proxOn;
    private bool _initialized;

    void Awake()
    {
        _renderers.AddRange(GetComponentsInChildren<Renderer>(true));
        foreach (var r in _renderers) _originalMats.Add(r.sharedMaterials);
        if (outlineChild) outlineChild.SetActive(false);
        _initialized = true;
    }

    // 兼容舊 API：視為 Aim 通道
    public void SetHighlighted(bool on) => SetAimHighlight(on);

    public void SetAimHighlight(bool on)
    {
        _aimOn = on;
        Apply();
    }

    public void SetProximityHighlight(bool on)
    {
        _proxOn = on;
        Apply();
    }

    private void Apply()
    {
        if (!_initialized) Awake();

        bool any = _aimOn || _proxOn;

        // 方案 A：子物件外框
        if (outlineChild) outlineChild.SetActive(any);

        // 方案 B：材質切換
        if (useMaterialSwitch)
        {
            // 先還原
            for (int i = 0; i < _renderers.Count; i++)
                _renderers[i].sharedMaterials = _originalMats[i];

            if (any)
            {
                var matToUse = _aimOn && aimOutlineMat ? aimOutlineMat
                              : (_proxOn && proximityOutlineMat ? proximityOutlineMat : aimOutlineMat);

                if (matToUse)
                {
                    for (int i = 0; i < _renderers.Count; i++)
                    {
                        var list = new List<Material>(_originalMats[i]);
                        if (!list.Contains(matToUse))
                        {
                            list.Add(matToUse);
                            _renderers[i].sharedMaterials = list.ToArray();
                        }
                    }
                }
            }
        }
    }

    private void OnDisable()
    {
        // 保險：物件隱藏時關掉
        _aimOn = _proxOn = false;
        Apply();
    }
}