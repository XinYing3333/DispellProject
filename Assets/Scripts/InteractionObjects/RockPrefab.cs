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
        
        [Header("VFX")]
        [SerializeField] private ParticleSystem hitVFX;

        private Rigidbody _rb;
        private int _interactionLayer;
        private int _hitTargetLayer;
        private bool _isThrown = false;
        
        // 只要未被投擲即可吸附
        public bool CanAttach => !_isThrown;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _interactionLayer = LayerMask.NameToLayer("InteractionMask");
            _hitTargetLayer = LayerMask.NameToLayer("Target");

            // 預設配置於可互動層
            gameObject.layer = _interactionLayer;

            if (targetRenderer != null && normalMaterials.Length == 0)
            {
                normalMaterials = targetRenderer.materials;
            }
            
            // 直接寫入 Payload，無須等待法術觸發
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

        #region IMagnetAttachable (吸附邏輯)
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

        #region IThrowable (投擲前置處理)
        public void OnBeforeThrow()
        {
            if (!_rb) return;

            transform.SetParent(null, true);
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.detectCollisions = true;
            
            _isThrown = true;
            gameObject.layer = _hitTargetLayer; // 投擲後轉換為 Target 層以正確觸發碰撞判定
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
        
                Shatter();
            }
            else
            {
                if (isBoss)
                {
                    Shatter();
                }
            }
        }

        private void Shatter()
        {
            if (hitVFX) hitVFX.Play();
            LandingAttack.ReturnRockToPool(gameObject);
        }
    }
}