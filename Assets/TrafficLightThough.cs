using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Player.InteractionSystem;
using SpellSystem;

namespace DefaultNamespace
{
    public class TraficLightThough : MonoBehaviour, ICollectable, ISpellAffectable, IMagnetAttachable
    {
        [Header("VFX & Model Settings")]
        [SerializeField] private Transform modelTransform; // 禁行標誌的模型位移目標
        [SerializeField] private float shakeStrength = 0.05f; // 顫抖強度
        [SerializeField] private ParticleSystem collectingVFX;
        
        [Header("Visual Overlay Settings")]
        [SerializeField] private Material overlayMaterial;
        [SerializeField] private float scaleMultiplier = 1.02f;
        private MeshRenderer meshRenderer;
        private List<GameObject> _overlayObjects = new List<GameObject>();
        
        private bool isStopSpellHit = false;
        private Tween _shakeTween; // 儲存顫抖動畫引用
        private Rigidbody _rb;
        private Vector3 _initialLocalPos; // 紀錄初始位置

        public bool NeedCollectAnimation => false;
        public bool IsCollectableActive => !isStopSpellHit; // 沒被打中時是收集品

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            _rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            if (collectingVFX != null) collectingVFX.Stop();
        }

        public void Collect()
        {
            if(isStopSpellHit) return;
            
            // 1. 處理粒子
            if (collectingVFX != null && !collectingVFX.isPlaying)
            {
                collectingVFX.Play();
            }

            // 2. 處理模型顫抖
            StartShake();
        }

        private void StartShake()
        {
            if (modelTransform == null) return;
    
            if (!DOTween.IsTweening(modelTransform))
            {
                // 紀錄目前的 localPosition，確保歸位時回到正確位置而非 (0,0,0)
                _initialLocalPos = modelTransform.localPosition;

                modelTransform.DOShakePosition(0.1f, shakeStrength, 15, 90, false, false)
                    .OnComplete(() => {
                        modelTransform.DOLocalMove(_initialLocalPos, 0.05f);
                    });
            }
        }

        public void OnSpellHit(SpellType spellType, Vector3 hitPoint)
        {
            if (spellType == SpellType.StopSpell)
            {
                SetMoveable();
                CreateVisualOverlays();
            }
        }

        public void OnSpellRecall()
        {
            DisableMoveable();
            RemoveVisualOverlays();
        }
        
        private void SetMoveable()
        {
            isStopSpellHit = true;
        }

        private void DisableMoveable()
        {
            isStopSpellHit = false;
        }
        private void CreateVisualOverlays()
        {
            if(meshRenderer) meshRenderer.enabled = false;

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
            if(meshRenderer) meshRenderer.enabled = true;
            foreach (var obj in _overlayObjects)
            {
                if (obj != null) Destroy(obj);
            }
            _overlayObjects.Clear();
        }
        
        public virtual void OnMagnetAttached(Transform parent)
        {
            if (!_rb) return;
            if (!isStopSpellHit) return;
            
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.detectCollisions = false;

            // 移動最頂層父物件，保持 Model 在 Traffic 內部的相對位置 (0,0,0)
            transform.root.SetParent(parent, true);
        }


        public virtual void OnMagnetDetached()
        {
            if (!_rb) return;
            if (!isStopSpellHit) return;
            
            transform.root.SetParent(null, true);
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.detectCollisions = true;
        }
    }
}