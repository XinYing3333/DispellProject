using System.Collections.Generic;
using SpellSystem;
using UnityEngine;

public class StoppablePlatform : MonoBehaviour, ISpellAffectable
{
    [Header("Visual Overlay Settings")]
    [SerializeField] private Material overlayMaterial;
    [SerializeField] private float scaleMultiplier = 1.02f;

    private Animator _anim;
    private List<GameObject> _overlayObjects = new List<GameObject>();
    
    // 快取所有帶有 Targetable 的 Transform，避免重複尋找
    private List<Transform> _targetableTransforms = new List<Transform>();
    private int _originalLayer;
    private int _defaultLayer;

    void Awake()
    {
        _anim = GetComponentInChildren<Animator>();
        _originalLayer = LayerMask.NameToLayer("Target");
        _defaultLayer = LayerMask.NameToLayer("Default");

        // 找出所有子物件中掛有 Targetable 腳本的 Transform
        Targetable[] tps = GetComponentsInChildren<Targetable>(true);
        foreach (var t in tps)
        {
            _targetableTransforms.Add(t.transform);
        }
    }

    public void OnSpellHit(SpellType spellType, Vector3 hitPoint)
    {
        if (spellType == SpellType.StopSpell)
        {
            StopObject();
            CreateVisualOverlays();
        }
    }

    public void OnSpellRecall()
    {
        ResumeObject();
        RemoveVisualOverlays();
    }

    private void StopObject()
    {
        if (_anim != null) _anim.speed = 0f;
        if (TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;

        // 將所有可瞄準子物件切換至 Default Layer，使其脫離 AimAssist 偵測
        SetTargetablesLayer(_defaultLayer);
    }

    private void ResumeObject()
    {
        if (_anim != null) _anim.speed = 1f;
        if (TryGetComponent(out Rigidbody rb)) rb.isKinematic = false;

        // 恢復至原始 Target Layer
        SetTargetablesLayer(_originalLayer);
    }

    private void SetTargetablesLayer(int layer)
    {
        foreach (var t in _targetableTransforms)
        {
            if (t != null)
            {
                t.gameObject.layer = layer;
                
                // 同時通知 Targetable 腳本取消當前高亮（避免 Layer 換了但 Outline 還在）
                if (t.TryGetComponent(out Targetable targetable))
                {
                    targetable.SetAimActive(false);
                }
            }
        }
    }

    private void CreateVisualOverlays()
    {
        if (_overlayObjects.Count > 0) return;

        MeshRenderer[] childRenderers = GetComponentsInChildren<MeshRenderer>();

        foreach (var renderer in childRenderers)
        {
            if (renderer.gameObject.name == "SpellOverlay") continue;

            MeshFilter mf = renderer.GetComponent<MeshFilter>();
            if (mf == null) continue;

            GameObject overlay = new GameObject("SpellOverlay");
            overlay.transform.SetParent(renderer.transform); 
            overlay.transform.localPosition = Vector3.zero;
            overlay.transform.localRotation = Quaternion.identity;
            overlay.transform.localScale = Vector3.one * scaleMultiplier;

            overlay.AddComponent<MeshFilter>().mesh = mf.mesh;
            MeshRenderer mr = overlay.AddComponent<MeshRenderer>();
            mr.material = overlayMaterial;
            
            overlay.layer = LayerMask.NameToLayer("Ignore Raycast");

            _overlayObjects.Add(overlay);
        }
    }

    private void RemoveVisualOverlays()
    {
        foreach (var obj in _overlayObjects)
        {
            if (obj != null) Destroy(obj);
        }
        _overlayObjects.Clear();
    }
}