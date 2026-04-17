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

        [Header("Collect Settings (一段一段吸收)")]
        [SerializeField] private float requiredCollect = 100f;     // 滿值 100
        [SerializeField] private float addAmountPerTick = 15f;     // 每次增加 15
        [SerializeField] private float collectInterval = 0.8f;     // 吸收間隔 0.8 秒
        [SerializeField, Tooltip("建議大於吸收間隔")] 
        private float autoHideDelay = 1.5f; // 停止吸收多久後隱藏介面
        
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
        private float _cooldownTimer; // ★ 新增：控制 0.8 秒間隔的計時器
        private bool _isSliderVisible = false;

        public bool NeedCollectAnimation => false;
        public bool IsSpellStateActive => false;

        private void Start()
        {
            if (DataManager.Instance.gameData.isTotemCollectSuccessDone)
            {
                spellSlot.SetActive(true);
                rockEffect.gameObject.SetActive(false);
                gameObject.SetActive(false);
                return;
            }
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

            // ★ 永遠都在倒數吸收的冷卻時間
            if (_cooldownTimer > 0)
            {
                _cooldownTimer -= Time.deltaTime;
            }

            // 處理沒有繼續吸收時的 UI 隱藏邏輯
            if (_isSliderVisible)
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

            // 只要外部有在呼叫（玩家按住按鍵），就重置隱藏計時器，保持 UI 顯示
            _hideTimer = autoHideDelay;
            if (!_isSliderVisible)
            {
                sliderCanvasGroup.DOKill();
                sliderCanvasGroup.DOFade(1, 0.2f);
                _isSliderVisible = true;
            }

            // ★ 判斷是否過了 0.8 秒的冷卻時間
            if (_cooldownTimer <= 0f)
            {
                ExecuteSingleCollectTick();
                
                // 重置冷卻時間，進入下一個 0.8 秒的等待
                _cooldownTimer = collectInterval; 
            }
        }

        // ★ 新增：單次吸收的具體執行邏輯
        private void ExecuteSingleCollectTick()
        {
            currentCollectCount += addAmountPerTick;
            if (slider != null) slider.value = currentCollectCount;

            // 每次吸收時觸發一次特效與震動
            if (collectingVFX != null)
            {
                collectingVFX.Stop(); // 先停掉再播，確保每次都有爆發感
                collectingVFX.Play();
            }
            StartShake();

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
            spellSlot.SetActive(true);
            
            EventBus<OnTutorialRequirementMet>.Raise(
                new OnTutorialRequirementMet { Requirement = TutorialRequirementType.TotemCollectSuccess });
            DataManager.Instance.gameData.isTotemCollectSuccessDone = true;

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
            // 當玩家主動放開按鍵時，可以選擇把冷卻時間歸零（這樣下次按會立刻吸收）
            // 如果你希望放開重按也要等，就把這行註解掉
            _cooldownTimer = 0f; 

            if (collectingVFX != null) collectingVFX.Stop();
            StopShake();
        }
    }
}