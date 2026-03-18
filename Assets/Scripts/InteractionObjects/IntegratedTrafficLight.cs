using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Player.InteractionSystem;
using SpellSystem;
using DefaultNamespace.Thought;

namespace DefaultNamespace
{
    public class IntegratedTrafficLight : MonoBehaviour, ICollectable, ISpellAffectable, IMagnetAttachable, IHitReceiver
    {
        [Header("Status")]
        [SerializeField] private bool isStopSpellHit = false;

        [Header("VFX & Model Settings (Collectable)")]
        [SerializeField] private Transform modelTransform;
        [SerializeField] private float shakeStrength = 0.05f;
        [SerializeField] private ParticleSystem collectingVFX;
        
        [Header("Visual Overlay Settings")]
        [SerializeField] private Material overlayMaterial;
        [SerializeField] private float scaleMultiplier = 1.02f;
        private MeshRenderer _mainMeshRenderer;
        private List<GameObject> _overlayObjects = new List<GameObject>();

        [Header("Hit Logic & Path (HitTarget)")]
        public RoadFader road;
        public Collider crossRoad;
        public Animator animator;
        public float fadeInTime = 1f;
        public float openSeconds = 6f;
        public float fadeOutTime = 1f;
        public UnityEvent onFirstHit;
        private bool _consumed;
        private bool _isPathBusy;

        [Header("UI (Countdown)")]
        public TextMeshProUGUI countdownText;
        public RectTransform countdownPulseTarget;
        public Color normalColor = Color.white;
        public Color dangerColor = Color.red;
        public int dangerThreshold = 3;
        
        [Header("UI Tween Settings")]
        public float pulseAmount = 0.15f;
        public float pulseDuration = 0.22f;
        public float showDuration = 0.25f;
        private Vector3 _uiOrigScale;
        private Tweener _showHideTween;

        private Rigidbody _rb;
        private Vector3 _initialLocalPos;
        private int _originalLayer;
        private int _interactionLayer;

        // Interface Properties
        public bool NeedCollectAnimation => false;
        public bool IsSpellStateActive => isStopSpellHit;

        private void Awake()
        {
            _mainMeshRenderer = GetComponent<MeshRenderer>();
            _rb = GetComponent<Rigidbody>();
            _originalLayer = LayerMask.NameToLayer("Target");
            _interactionLayer = LayerMask.NameToLayer("InteractionMask");
            
            if (countdownPulseTarget)
            {
                _uiOrigScale = countdownPulseTarget.localScale;
                countdownPulseTarget.localScale = Vector3.zero;
            }
            if (crossRoad) crossRoad.enabled = false;
        }

        #region ICollectable (一般狀態下的吸收反應)
        public void Collect()
        {
            // 法術狀態下禁止吸收顫抖
            if (isStopSpellHit) return;

            if (collectingVFX != null && !collectingVFX.isPlaying)
                collectingVFX.Play();

            StartShake();
        }

        private void StartShake()
        {
            if (modelTransform == null || DOTween.IsTweening(modelTransform)) return;
            
            _initialLocalPos = modelTransform.localPosition;
            modelTransform.DOShakePosition(0.1f, shakeStrength, 15, 90)
                .OnComplete(() => modelTransform.DOLocalMove(_initialLocalPos, 0.05f));
        }
        #endregion

        #region IHitReceiver (一般狀態下的擊中路徑邏輯)
        public void OnHit(ThoughtPayloadSO payload)
        {
            // 法術狀態下禁止觸發倒數路徑
            if (isStopSpellHit || _isPathBusy) return;

            if (animator) animator.SetTrigger("Hit");
            StartCoroutine(RunPathCycle());
        }

        private IEnumerator RunPathCycle()
        {
            _isPathBusy = true;
            if (road) yield return road.FadeIn(fadeInTime);
            
            if (!_consumed)
            {
                _consumed = true;
                onFirstHit?.Invoke();
            }

            if (crossRoad) crossRoad.enabled = true;
            yield return StartCoroutine(Co_CountdownUI(Mathf.CeilToInt(openSeconds)));
            
            if (road) yield return road.FadeOut(fadeOutTime);
            if (crossRoad) crossRoad.enabled = false;
            _isPathBusy = false;
        }
        #endregion

        #region ISpellAffectable (狀態切換)
        public void OnSpellHit(SpellType spellType, Vector3 hitPoint)
        {
            if (spellType == SpellType.StopSpell)
            {
                isStopSpellHit = true;
                gameObject.layer = _interactionLayer;
                animator.speed = 0f;
                CreateVisualOverlays();
            }
        }

        public void OnSpellRecall()
        {
            isStopSpellHit = false;
            gameObject.layer = _originalLayer;
            animator.speed = 1f;
            RemoveVisualOverlays();
        }
        #endregion

        #region IMagnetAttachable (法術狀態下的移動)
        public void OnMagnetAttached(Transform parent)
        {
            if (!_rb || !isStopSpellHit) return;
            
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.detectCollisions = false;
            transform.root.SetParent(parent, true);
        }

        public void OnMagnetDetached()
        {
            if (!_rb || !isStopSpellHit) return;
            
            transform.root.SetParent(null, true);
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.detectCollisions = true;
        }
        #endregion

        #region UI & Overlays Helpers
        private IEnumerator Co_CountdownUI(int seconds)
        {
            ToggleUI(true);
            for (int i = seconds; i > 0; i--)
            {
                if (countdownText)
                {
                    countdownText.text = i.ToString();
                    countdownText.color = (i <= dangerThreshold) ? dangerColor : normalColor;
                }
                PulseUI();
                yield return new WaitForSeconds(1f);
            }
            ToggleUI(false);
        }

        private void ToggleUI(bool show)
        {
            if (!countdownPulseTarget) return;
            _showHideTween?.Kill();
            _showHideTween = countdownPulseTarget.DOScale(show ? _uiOrigScale : Vector3.zero, showDuration)
                .SetEase(show ? Ease.OutBack : Ease.InBack);
        }

        private void PulseUI()
        {
            if (!countdownPulseTarget) return;
            countdownPulseTarget.DOPunchScale(Vector3.one * pulseAmount, pulseDuration);
        }
        
        private void CreateVisualOverlays()
        {
            if (_mainMeshRenderer) _mainMeshRenderer.enabled = false;
            if (_overlayObjects.Count > 0) return;

            foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.gameObject.name == "SpellOverlay") continue;
                MeshFilter mf = renderer.GetComponent<MeshFilter>();
                if (!mf) continue;

                GameObject overlay = new GameObject("SpellOverlay");
                overlay.transform.SetParent(renderer.transform);
                overlay.transform.localPosition = Vector3.zero;
                overlay.transform.localScale = Vector3.one * scaleMultiplier;
                overlay.AddComponent<MeshFilter>().mesh = mf.mesh;
                overlay.AddComponent<MeshRenderer>().material = overlayMaterial;
                overlay.layer = LayerMask.NameToLayer("Ignore Raycast");
                _overlayObjects.Add(overlay);
            }
        }

        private void RemoveVisualOverlays()
        {
            if (_mainMeshRenderer) _mainMeshRenderer.enabled = true;
            foreach (var obj in _overlayObjects) if (obj) Destroy(obj);
            _overlayObjects.Clear();
        }
        #endregion
    }
}