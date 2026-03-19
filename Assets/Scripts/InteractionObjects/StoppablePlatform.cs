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
    
    private List<Transform> _targetableTransforms = new List<Transform>();
    private int _originalLayer;
    private int _defaultLayer;

    void Awake()
    {
        _anim = GetComponentInChildren<Animator>();
        // 建議確保這裡的 Layer 名稱與 AimAssist 的 InteractionMask 一致
        _originalLayer = LayerMask.NameToLayer("Target");
        _defaultLayer = LayerMask.NameToLayer("Default");

        Targetable[] tps = GetComponentsInChildren<Targetable>(true);
        foreach (var t in tps)
        {
            _targetableTransforms.Add(t.transform);
        }
    }

    public void OnSpellHit(SpellType spellType, Vector3 hitPoint)
    {
        // 假設 StopSpell 會讓物件進入停止狀態並不可再被瞄準
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
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // 停止時，切換 Layer 讓 AimAssist 找不到它
        SetTargetablesLayer(_defaultLayer);
    }

    private void ResumeObject()
    {
        if (_anim != null) _anim.speed = 1f;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        // 恢復時，切換回 Target Layer 重新允許瞄準
        SetTargetablesLayer(_originalLayer);
    }

    private void SetTargetablesLayer(int layer)
    {
        foreach (var t in _targetableTransforms)
        {
            if (t == null) continue;
            
            t.gameObject.layer = layer;
            
            // 【關鍵修改】：呼叫新的狀態接口
            if (t.TryGetComponent(out Targetable targetable))
            {
                // 當物件被停止或 Layer 改變時，強制關閉所有高亮狀態
                targetable.SetTargetState(TargetState.None);
            }
        }
    }

    // CreateVisualOverlays 與 RemoveVisualOverlays 邏輯維持不變...
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