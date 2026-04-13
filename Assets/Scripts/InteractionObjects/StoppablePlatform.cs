using System.Collections.Generic;
using DefaultNamespace.Tutorial;
using EventBus.Events.Tutorial;
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
    
    private static bool isStoppingBefore;

    void Awake()
    {
        _anim = GetComponentInChildren<Animator>();
        // 建議確保這裡的 Layer 名稱與 AimAssist 的 InteractionMask 一致
        _originalLayer = LayerMask.NameToLayer("Target");
        _defaultLayer = LayerMask.NameToLayer("Environment");

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
            
            if (!isStoppingBefore)
            {
                EventBus<OnTutorialRequirementMet>.Raise(
                    new OnTutorialRequirementMet { Requirement = TutorialRequirementType.ThrowAStoppablePlatform });
                DataManager.Instance.CommitSessionData();
            }
        }
    }

    public void OnSpellRecall()
    {
        ResumeObject();
        RemoveVisualOverlays();
    }

    private void StopObject()
    {
        if (_anim != null) 
        {
            // 取得當前狀態資訊
            var stateInfo = _anim.GetCurrentAnimatorStateInfo(0);
            _anim.speed = 0f;
        
            // 強制 Animator 停在當前時間點並立即採樣
            _anim.Play(stateInfo.fullPathHash, 0, stateInfo.normalizedTime);
            _anim.Update(0f); 
        
            Debug.Log($"[StoppablePlatform] Animator Forced Sampled at {stateInfo.normalizedTime}");
        }
        SetTargetablesLayer(_defaultLayer);
    }

    private void ResumeObject()
    {
        if (_anim != null)
        {
            _anim.speed = 1f;
        }

        
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

    private void CreateVisualOverlays()
    {
        if (_overlayObjects.Count > 0) return;
        MeshRenderer[] childRenderers = GetComponentsInChildren<MeshRenderer>();
    
        foreach (var renderer in childRenderers)
        {
            if (renderer.gameObject.name == "SpellOverlay") continue;
            MeshFilter mf = renderer.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            GameObject overlay = new GameObject("SpellOverlay");
            overlay.transform.SetParent(renderer.transform); 
            overlay.transform.localPosition = Vector3.zero;
            overlay.transform.localRotation = Quaternion.identity;
            overlay.transform.localScale = Vector3.one * scaleMultiplier;
        
            // 使用 sharedMesh 避免實例化開銷
            overlay.AddComponent<MeshFilter>().mesh = mf.sharedMesh;
            MeshRenderer mr = overlay.AddComponent<MeshRenderer>();
        
            // 強制賦值材質
            mr.sharedMaterial = overlayMaterial;
            overlay.layer = LayerMask.NameToLayer("Ignore Raycast");
        
            // 確保 Renderer 是啟動狀態
            mr.enabled = true;
        
            _overlayObjects.Add(overlay);
        }
        Debug.Log($"[StoppablePlatform] {_overlayObjects.Count} Overlays Created and Enabled.");
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