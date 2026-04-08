using DefaultNamespace.Tutorial;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using EventBus.Events.Tutorial;
using Player.InteractionSystem;

namespace DefaultNamespace
{
    public class StopSignThought : MonoBehaviour, ICollectable
    {
        [Header("UI Reference")]
        [SerializeField] private Slider slider;
        [SerializeField] private CanvasGroup sliderCanvasGroup;
        [SerializeField] private TotemDiscoveryUI totemUI; 

        [Header("Collect Settings")]
        [SerializeField] private float requiredCollect = 1f; 
        [SerializeField] private float addCollectCount = 1f; 
        [SerializeField] private float autoHideDelay = 0.5f; 
        [SerializeField] private ObstacleGroup rockEffect;

        [Header("VFX & Model Settings")]
        [SerializeField] private Transform modelTransform; 
        [SerializeField] private float shakeStrength = 0.05f; 
        [SerializeField] private ParticleSystem collectingVFX;
        [SerializeField] private ParticleSystem completeVFXPrefab;
        [SerializeField] private float destroyDelay = 0.5f;
        
        [SerializeField] private GameObject spellSlot;
        
        private float currentCollectCount;
        private bool isCompleted = false;
        private float _hideTimer;
        private bool _isSliderVisible = false;
        private bool _isBeingCollected = false;

        public bool NeedCollectAnimation => false;
        public bool IsSpellStateActive => false;

        private void Start()
        {
            if (slider != null)
            {
                slider.maxValue = requiredCollect;
                slider.value = 0;
                sliderCanvasGroup.alpha = 0;
            }
            if (collectingVFX != null) collectingVFX.Stop();
            spellSlot.SetActive(false);
        }

        private void Update()
        {
            if (isCompleted) return;

            if (_isBeingCollected)
            {
                UpdateProgress();
                _isBeingCollected = false;
            }
            else if (_isSliderVisible)
            {
                _hideTimer -= Time.deltaTime;
                if (_hideTimer <= 0)
                {
                    HideSlider();
                    StopShake();
                }
            }
        }

        public void Collect()
        {
            if (isCompleted) return;

            _isBeingCollected = true;
            _hideTimer = autoHideDelay;
            
            if (collectingVFX != null && !collectingVFX.isPlaying)
            {
                collectingVFX.Play();
            }

            StartShake();

            if (!_isSliderVisible)
            {
                sliderCanvasGroup.DOKill();
                sliderCanvasGroup.DOFade(1, 0.2f);
                _isSliderVisible = true;
            }
        }

        private void UpdateProgress()
        {
            currentCollectCount += Time.deltaTime * addCollectCount;
            if (slider != null) slider.value = currentCollectCount;

            if (currentCollectCount >= requiredCollect)
            {
                CompleteCollection();
            }
        }

        private void StartShake()
        {
            if (modelTransform == null) return;
            if (!DOTween.IsTweening(modelTransform))
            {
                modelTransform.DOShakePosition(0.1f, shakeStrength, 15, 90, false, false)
                    .OnComplete(() => {
                        modelTransform.DOLocalMove(Vector3.zero, 0.05f);
                    });
            }
        }

        private void StopShake()
        {
            if (DOTween.IsTweening(modelTransform))
            {
                modelTransform.DOKill();
                modelTransform.DOLocalMove(Vector3.zero, 0.05f);
            }
        }

        private void HideSlider()
        {
            _isSliderVisible = false;
            sliderCanvasGroup.DOKill();
            sliderCanvasGroup.DOFade(0, 0.5f);
            if (collectingVFX != null) collectingVFX.Stop();
        }

        private void CompleteCollection()
        {
            isCompleted = true;
            _isBeingCollected = false;
            _isSliderVisible = false;

            StopShake();
            if (collectingVFX != null) collectingVFX.Stop();
            
            if (completeVFXPrefab != null)
            {
                ParticleSystem vfx = Instantiate(completeVFXPrefab, transform.position, Quaternion.identity);
                vfx.Play();
                Destroy(vfx.gameObject, 3f);
            }

            sliderCanvasGroup.DOKill();
            sliderCanvasGroup.DOFade(0, 0.3f);
            
            CollectionSystem.CollectItem(CollectionSystem.CollectedType.StopSignThough, 1);
            
            // 優先進入 UI 流程
            if (totemUI != null)
            {
                totemUI.gameObject.SetActive(true);
                totemUI.Show(FinalizeEffect);
            }
            else
            {
                FinalizeEffect();
            }
        }

        private void FinalizeEffect()
        {
            // 此處為 UI 消失後的執行點（此時 TimeScale 已恢復 1）
            spellSlot.SetActive(true);
            
            EventBus<OnTutorialRequirementMet>.Raise(
                new OnTutorialRequirementMet { Requirement = TutorialRequirementType.TotemCollectSuccess });
            
            if (rockEffect != null) rockEffect.OnInteract();

            DisableObjectState();
            Destroy(gameObject, destroyDelay);
        }

        private void DisableObjectState()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.enabled = false;
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var c in colliders) c.enabled = false;
        }

        public void StopCollect()
        {
            if (isCompleted) return;
            _isBeingCollected = false;
            if (collectingVFX != null) collectingVFX.Stop();
            StopShake();
        }
    }
}