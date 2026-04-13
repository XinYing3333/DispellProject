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
using DefaultNamespace.Tutorial;
using EventBus.Events.Tutorial;

namespace DefaultNamespace
{
    public class TrafficLightThought : MonoBehaviour, ICollectable, ISpellAffectable, IMagnetAttachable, IHitReceiver
    {
        [Header("Status")]
        [SerializeField] private bool isStopSpellHit = false;

        [Header("VFX & Model Settings (Collectable)")]
        [SerializeField] private Transform modelTransform;
        [SerializeField] private float shakeStrength = 0.05f;
        [SerializeField] private ParticleSystem collectingVFX;
        [SerializeField] private ParticleSystem hitVFX;
        
        [Header("Visual Material Settings")]
        [SerializeField] private SkinnedMeshRenderer targetRenderer; // 指定要替換材質的 Renderer
        [SerializeField] private Material[] normalMaterials;  // 一般狀態下的材質陣列
        [SerializeField] private Material[] spellHitMaterials; // 被法術擊中時的材質陣列
        [SerializeField] private Material[] objectHitMaterials; // 被物品擊中時的材質陣列

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
        private bool _isInActiveArea;

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
        private int _trafficLightActiveArea;

        private static bool isHittingBefore;

        // Interface Properties
        public bool NeedCollectAnimation => false;
        public bool IsSpellStateActive => isStopSpellHit;
        public bool CanAttach { get; set; } = true;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _originalLayer = LayerMask.NameToLayer("Target");
            _interactionLayer = LayerMask.NameToLayer("InteractionMask");
            _trafficLightActiveArea = LayerMask.NameToLayer("TrafficLightActiveArea");
            
            if (countdownPulseTarget)
            {
                _uiOrigScale = countdownPulseTarget.localScale;
                countdownPulseTarget.localScale = Vector3.zero;
            }
            if (crossRoad) crossRoad.enabled = false;

            // 啟動時自動快取目標 Renderer 目前的材質為 normalMaterials（若未手動指定）
            if (targetRenderer != null && normalMaterials.Length == 0)
            {
                normalMaterials = targetRenderer.materials;
            }
        }

        #region ICollectable (一般狀態下的吸收反應)
        public void Collect()
        {
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
            // 加入 !_isInActiveArea 阻斷邏輯
            if (isStopSpellHit || _isPathBusy || !_isInActiveArea) return;

            if (!isHittingBefore)
            {
                EventBus<OnTutorialRequirementMet>.Raise(
                    new OnTutorialRequirementMet { Requirement = TutorialRequirementType.FirstThrowTraffic });
                DataManager.Instance.CommitSessionData();

            }
            if (animator) animator.SetTrigger("Hit");
            SwapMaterials(objectHitMaterials);
            if (hitVFX) hitVFX.Play();
            StartCoroutine(RunPathCycle());
        }
        private IEnumerator RunPathCycle()
        {
            _isPathBusy = true;
            if (crossRoad) crossRoad.enabled = true;
            if (road) yield return road.FadeIn(fadeInTime);
            
            if (!_consumed)
            {
                _consumed = true;
                onFirstHit?.Invoke();
            }
            yield return StartCoroutine(Co_CountdownUI(Mathf.CeilToInt(openSeconds)));
            
            SwapMaterials(normalMaterials);
            if (animator) animator.SetTrigger("Red");
            if (road) yield return road.FadeOut(fadeOutTime);
            if (crossRoad) crossRoad.enabled = false;
            _isPathBusy = false;
        }
        #endregion

        #region ISpellAffectable (狀態切換)
        public void OnSpellHit(SpellType spellType, Vector3 hitPoint)
        {
            // if (spellType == SpellType.StopSpell)
            // {
            //     isStopSpellHit = true;
            //     gameObject.layer = _interactionLayer;
            //     animator.speed = 0f;
            //     SwapMaterials(spellHitMaterials);
            // }
        }

        public void OnSpellRecall()
        {
            // isStopSpellHit = false;
            // gameObject.layer = _originalLayer;
            // animator.speed = 1f;
            // SwapMaterials(normalMaterials);
        }
        #endregion

        #region IMagnetAttachable (法術狀態下的移動)
        public void OnMagnetAttached(Transform parent)
        {
            // if (!_rb || !isStopSpellHit) return;
            //
            // _rb.isKinematic = true;
            // _rb.useGravity = false;
            // _rb.detectCollisions = false;
            // transform.root.SetParent(parent, true);
        }

        public void OnMagnetDetached()
        {
            // if (!_rb || !isStopSpellHit) return;
            //
            // transform.root.SetParent(null, true);
            // _rb.isKinematic = false;
            // _rb.useGravity = true;
            // _rb.detectCollisions = true;
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
        
        private void SwapMaterials(Material[] newMaterials)
        {
            if (targetRenderer == null || newMaterials == null || newMaterials.Length == 0) return;
            targetRenderer.materials = newMaterials;
        }
        #endregion

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == _trafficLightActiveArea)
            {
                _isInActiveArea = true;
            }
        }

        // 必須新增 Exit 以重置狀態
        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == _trafficLightActiveArea)
            {
                _isInActiveArea = false;
            }
        }
    }
}