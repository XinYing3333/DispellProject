using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Player.InteractionSystem;

namespace DefaultNamespace
{
    public class StopSignThought : MonoBehaviour, ICollectable
    {
        [Header("UI Reference")]
        [SerializeField] private Slider slider;
        [SerializeField] private CanvasGroup sliderCanvasGroup;

        [Header("Collect Settings")]
        [SerializeField] private float requiredCollect = 1f; 
        [SerializeField] private float addCollectCount = 1f; 
        [SerializeField] private float autoHideDelay = 0.5f; // 縮短延遲以配合操作感
        [SerializeField] private ObstacleGroup rockEffect;

        [Header("VFX & Model Settings")]
        [SerializeField] private Transform modelTransform; // 禁行標誌的模型位移目標
        [SerializeField] private float shakeStrength = 0.05f; // 顫抖強度
        [SerializeField] private ParticleSystem collectingVFX;
        [SerializeField] private ParticleSystem completeVFXPrefab;
        [SerializeField] private float destroyDelay = 0.5f;

        private float currentCollectCount;
        private bool isCompleted = false;
        private float _hideTimer;
        private bool _isSliderVisible = false;
        private Tween _shakeTween; // 儲存顫抖動畫引用

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
        }

        private void Update()
        {
            if (isCompleted || !_isSliderVisible) return;

            _hideTimer -= Time.deltaTime;
            if (_hideTimer <= 0)
            {
                HideSlider();
                StopShake(); // 停止顫抖
            }
        }

        public void Collect()
        {
            if (isCompleted) return;

            _hideTimer = autoHideDelay;
            
            // 1. 處理粒子
            if (collectingVFX != null && !collectingVFX.isPlaying)
            {
                collectingVFX.Play();
            }

            // 2. 處理模型顫抖
            StartShake();

            // 3. 處理 UI
            if (!_isSliderVisible)
            {
                sliderCanvasGroup.DOKill();
                sliderCanvasGroup.DOFade(1, 0.2f);
                _isSliderVisible = true;
            }

            currentCollectCount += Time.deltaTime * addCollectCount;
            slider.value = currentCollectCount;

            if (currentCollectCount >= requiredCollect)
            {
                CompleteCollection();
            }
        }

        private void StartShake()
        {
            if (modelTransform == null) return;
    
            // 檢查目前是否正在進行顫抖動畫，如果正在動就不重複觸發
            // 這樣能確保一次抖動動畫完整跑完 (例如 0.1s)，才接下一次
            if (!DOTween.IsTweening(modelTransform))
            {
                modelTransform.DOShakePosition(0.1f, shakeStrength, 15, 90, false, false)
                    .OnComplete(() => {
                        // 每段抖動完畢後微調回原位，確保座標不偏移
                        modelTransform.DOLocalMove(Vector3.zero, 0.05f);
                    });
            }
        }

        private void StopShake()
        {
            // 停止 Collect 時，如果還在抖，可以讓它播完最後一次，或者立即 Kill
            // 若要立即停止：
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
            StopShake(); // 完成時停止顫抖
            
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
            
            if (rockEffect != null)
            {
                rockEffect.OnInteract();
            }

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
            if (collectingVFX != null) collectingVFX.Stop();
            StopShake();
        }
    }
}