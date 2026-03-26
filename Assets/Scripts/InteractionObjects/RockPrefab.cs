using DefaultNamespace.Thought;
using UnityEngine;
using Player.InteractionSystem;
using SpellSystem;
using DG.Tweening;
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    [RequireComponent(typeof(Rigidbody))]
    public class RockPrefab : MonoBehaviour, IMagnetAttachable, ISpellAffectable, IThrowable
    {
        [Header("Status")]
        [SerializeField] private bool isStopped = false;
        [SerializeField] private ThrowProjectile _currentProjectile;
        [SerializeField] private ThoughtPayloadSO _requiredPayloadSo;

        [Header("Visual Settings")]
        [SerializeField] private MeshRenderer targetRenderer;
        [SerializeField] private Material[] normalMaterials;
        [SerializeField] private Material[] spellHitMaterials;
        
        [Header("VFX")]
        [SerializeField] private ParticleSystem hitVFX;

        private Rigidbody _rb;
        private int _originalLayer;
        private int _interactionLayer;
        private int _hitTargetLayer;
        
        public bool CanAttach => isStopped;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _interactionLayer = LayerMask.NameToLayer("InteractionMask");
            _hitTargetLayer = LayerMask.NameToLayer("Target");
            _originalLayer = _hitTargetLayer;

            if (targetRenderer != null && normalMaterials.Length == 0)
            {
                // 這裡使用 materials 而非 sharedMaterials 避免污染 Editor 資源
                normalMaterials = targetRenderer.materials;
            }
        }

        private void OnDisable()
        {
            // 當物件被收回池中時，重設所有狀態
            ResetRockState();
        }

        private void ResetRockState()
        {
            isStopped = false;
            if (_currentProjectile != null) _currentProjectile.payload = null;
            
            gameObject.layer = _originalLayer;
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

        #region ISpellAffectable (靜止念頭邏輯)
        public void OnSpellHit(SpellType spellType, Vector3 hitPoint)
        {
            if (spellType == SpellType.StopSpell)
            {
                isStopped = true;
                if (_currentProjectile != null) _currentProjectile.payload = _requiredPayloadSo;
                gameObject.layer = _interactionLayer;
                
                if (hitVFX) hitVFX.Play();
                SwapMaterials(spellHitMaterials);
            }
        }

        public void OnSpellRecall()
        {
            isStopped = false;
            if (_currentProjectile != null) _currentProjectile.payload = null;
            gameObject.layer = _hitTargetLayer;
            SwapMaterials(normalMaterials);
        }
        #endregion

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
            
            if (!isStopped)
            {
                _rb.isKinematic = false;
                _rb.useGravity = true;
                _rb.detectCollisions = true;
            }
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
            
            isStopped = false;
            gameObject.layer = _originalLayer;
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
            var hitReceiver = collision.gameObject.GetComponentInParent<IHitReceiver>();

            if (hitReceiver != null)
            {
                // 2. 觸發 Boss 的受傷邏輯並傳遞 Payload (這裡傳遞石頭自帶的 Payload)
                hitReceiver.OnHit(_requiredPayloadSo);

                // 3. 回收石頭
                LandingAttack.ReturnRockToPool(this.gameObject);
            }
            // 碰撞 Boss 後回收至物件池，而非 Destroy
            if (collision.gameObject.CompareTag("boss"))
            {
                LandingAttack.ReturnRockToPool(this.gameObject);
            }
        }
    }
}