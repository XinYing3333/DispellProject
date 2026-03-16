using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Player.InteractionSystem;

namespace DefaultNamespace
{
    public class StopSignThough : MonoBehaviour, ICollectable
    {
        [Header("UI Reference")]
        [SerializeField] private Slider slider;
        [SerializeField] private CanvasGroup sliderCanvasGroup;

        [Header("Collect Settings")]
        [SerializeField] private float requiredCollect = 1f; 
        [SerializeField] private float addCollectCount = 1f; 
        [SerializeField] private float autoHideDelay = 5f;
        [SerializeField] private ObstacleGroup rockEffect;

        [Header("VFX Settings")]
        [SerializeField] private ParticleSystem collectingVFX; // 持續收集的粒子 (例如吸附氣流)
        [SerializeField] private ParticleSystem completeVFXPrefab; // 完成時生成的粒子預製體
        [SerializeField] private float destroyDelay = 0.5f; // 延遲銷毀時間

        private float currentCollectCount;
        private bool isCompleted = false;
        private float _hideTimer;
        private bool _isSliderVisible = false;
        
        public bool NeedCollectAnimation => false;

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
                if (collectingVFX != null) collectingVFX.Stop(); // 停止收集粒子
            }
        }

        public void Collect()
        {
            if (isCompleted) return;

            _hideTimer = autoHideDelay;
            
            // 處理粒子播放
            if (collectingVFX != null)
            {
                collectingVFX.Play();
            }

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
            
            // 1. 粒子表現
            if (collectingVFX != null) collectingVFX.Stop();
            if (completeVFXPrefab != null)
            {
                ParticleSystem vfx = Instantiate(completeVFXPrefab, transform.position, Quaternion.identity);
                vfx.Play();
                Destroy(vfx.gameObject, 3f); // 粒子自動回收
            }

            // 2. UI 表現
            sliderCanvasGroup.DOKill();
            sliderCanvasGroup.DOFade(0, 0.3f);
            
            // 3. 系統邏輯
            CollectionSystem.CollectItem(CollectionSystem.CollectedType.StopSignThough, 1);
            
            if (rockEffect != null)
            {
                rockEffect.OnInteract();
            }

            // 4. 物件銷毀
            // 先關閉渲染與碰撞，避免干擾，隨後銷毀
            DisableObjectState();
            Destroy(gameObject, destroyDelay);
        }

        private void DisableObjectState()
        {
            // 禁用所有 Renderer 和 Collider
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.enabled = false;
            
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var c in colliders) c.enabled = false;
        }

        public void StopCollect()
        {
            if (collectingVFX != null) collectingVFX.Stop();
        }
    }
}