using DefaultNamespace.Thought;
using UnityEngine;
using Player.InteractionSystem;
using DG.Tweening;
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    [RequireComponent(typeof(Rigidbody))]
    public class RockPrefab : MonoBehaviour, IMagnetAttachable, IThrowable
    {
        [Header("Status")]
        [SerializeField] private ThrowProjectile _currentProjectile;
        [SerializeField] private ThoughtPayloadSO _requiredPayloadSo;

        [Header("Visual Settings")]
        [SerializeField] private MeshRenderer targetRenderer;
        [SerializeField] private Material[] normalMaterials;
        
        [Header("VFX & Break Effects")]
        [SerializeField] private ParticleSystem hitVFX;
        [Tooltip("包含獨立碎片剛體的破碎預製物件")]
        [SerializeField] private GameObject fracturedPrefab; 
        [SerializeField] private float fadeDelay = 1.5f;
        [SerializeField] private float shrinkDuration = 0.5f;

        private Rigidbody _rb;
        private int _interactionLayer;
        private int _hitTargetLayer;
        private bool _isThrown = false;
        private Vector3 _originalScale;
        
        public bool CanAttach => !_isThrown;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _interactionLayer = LayerMask.NameToLayer("InteractionMask");
            _hitTargetLayer = LayerMask.NameToLayer("Target");

            gameObject.layer = _interactionLayer;
            _originalScale = transform.localScale;

            if (targetRenderer != null && normalMaterials.Length == 0)
            {
                normalMaterials = targetRenderer.materials;
            }
            
            if (_currentProjectile != null) 
            {
                _currentProjectile.payload = _requiredPayloadSo;
            }
        }

        private void OnDisable()
        {
            ResetRockState();
        }

        private void ResetRockState()
        {
            _isThrown = false;
            gameObject.layer = _interactionLayer;
            SwapMaterials(normalMaterials);
            transform.localScale = _originalScale;

            if (targetRenderer != null) targetRenderer.enabled = true;

            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.useGravity = true;
                _rb.detectCollisions = true;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            
            transform.SetParent(null);
        }

        #region IMagnetAttachable
        public void OnMagnetAttached(Transform parent)
        {
            if (!_rb) return;
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.detectCollisions = false;
            transform.SetParent(parent, true);
        }

        public void OnMagnetDetached()
        {
            if (!_rb) return;
            transform.SetParent(null, true);
            
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.detectCollisions = true;
        }
        #endregion

        #region IThrowable
        public void OnBeforeThrow()
        {
            if (!_rb) return;

            transform.SetParent(null, true);
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.detectCollisions = true;
            
            _isThrown = true;
            gameObject.layer = _hitTargetLayer; 
            SwapMaterials(normalMaterials);
        }
        #endregion

        private void SwapMaterials(Material[] newMaterials)
        {
            if (targetRenderer == null || newMaterials == null || newMaterials.Length == 0) return;
            targetRenderer.materials = newMaterials;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player")) return;

            bool isBoss = collision.gameObject.CompareTag("boss");
            Vector3 contactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
    
            if (_isThrown)
            {
                if (isBoss)
                {
                    var hitReceiver = collision.gameObject.GetComponentInParent<IHitReceiver>();
                    if (hitReceiver != null)
                    {
                        hitReceiver.OnHit(_requiredPayloadSo);
                    }
                }
        
                Shatter(contactPoint);
            }
            else
            {
                if (isBoss)
                {
                    Shatter(contactPoint);
                }
            }
        }

        private void Shatter(Vector3 spawnPosition)
        {
            if (hitVFX)
            {
                hitVFX.transform.position = spawnPosition;
                hitVFX.Play();
            }

            if (fracturedPrefab != null)
            {
                // 1. 以原本石頭的世界座標與旋轉生成破碎根物件
                GameObject fxObj = Instantiate(fracturedPrefab, transform.position, transform.rotation);
        
                // 2. 確保根物件的本地縮放與當前主石頭完全一致
                fxObj.transform.localScale = transform.localScale;

                Rigidbody[] pieces = fxObj.GetComponentsInChildren<Rigidbody>();
        
                // 建立專用動畫序列
                Sequence seq = DOTween.Sequence();
                seq.AppendInterval(fadeDelay);

                foreach (var piece in pieces)
                {
                    // 驅動物理散射
                    piece.AddExplosionForce(150f, spawnPosition, 2f);
                    if (_rb != null)
                    {
                        piece.linearVelocity += _rb.linearVelocity * 0.5f;
                    }

                    // 3. 核心修正：明確記錄當前碎片正確的本地縮放，以此為動畫基準起點
                    Vector3 targetInitialScale = piece.transform.localScale;

                    // 4. 強制將縮放從正確的初始值漸變至零，阻斷任何起點偵測錯誤
                    seq.Join(piece.transform.DOScale(Vector3.zero, shrinkDuration)
                        .From(targetInitialScale) 
                        .SetEase(Ease.InQuad));
                }
        
                seq.OnComplete(() => Destroy(fxObj));
            }

            if (targetRenderer != null) targetRenderer.enabled = false;
            if (_rb != null) _rb.detectCollisions = false;
    
            LandingAttack.ReturnRockToPool(gameObject);
        }
    }
}