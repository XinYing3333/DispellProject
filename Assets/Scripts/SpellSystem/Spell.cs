using System.Collections;
using SpellSystem;
using UnityEngine;
using UnityEngine.VFX; 

[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class Spell : MonoBehaviour
{
    [Header("Type Definition")]
    public SpellType spellType; 

    [Header("Visual / FX (Pre-placed in Prefab)")]
    [SerializeField] private VisualEffect travelVFX;    // 拖入子物件中的 VFX Graph
    [SerializeField] private ParticleSystem hitVFX;    // 拖入子物件中的 Particle System
    public SFXType explodeSfx = SFXType.Spawn;

    [Header("Flight")]
    public bool enableHoming = false;
    public float homingSpeed = 8f;
    public float rotateSpeed = 8f;
    public float fuseLifetime = 0.35f;

    [Header("Impact")]
    public bool explodeOnCollision = true;
    public LayerMask collideMask = ~0;

    [Header("Cleanup")]
    public float cleanupDelay = 1.0f; // 增加延遲以確保 HitParticle 播完

    // --- 內部狀態 ---
    private Rigidbody _rb;
    private Collider _col;
    private MeshRenderer _mesh;

    private float _life;
    private bool _exploded;
    private Transform _target;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        _mesh = GetComponent<MeshRenderer>();
        _life = fuseLifetime;

        // 初始化狀態
        if (travelVFX) travelVFX.Play();
        if (hitVFX) hitVFX.Stop(); 
    }

    void Start()
    {
        ApplySpellTypeSettings();
    }

    private void ApplySpellTypeSettings()
    {
        if (_mesh)
        {
            switch (spellType)
            {
                case SpellType.StopSpell:     
                    _mesh.material.color = new Color(1f, 0.8f, 0.2f); 
                    break;
                case SpellType.AttackSpell:     
                    _mesh.material.color = new Color(1f, 0.2f, 0.2f); 
                    break;
            }
        }
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    void Update()
    {
        if (_exploded) return;

        _life -= Time.deltaTime;
        if (_life <= 0f)
        {
            Explode();
        }
    }

    void FixedUpdate()
    {
        if (_exploded) return;
        if (!enableHoming || _target == null) return;

        Vector3 dir = (_target.position - transform.position);
        if (dir.sqrMagnitude < 1e-6f) return;

        dir.Normalize();
        Vector3 desiredVel = dir * homingSpeed;
        _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, desiredVel, rotateSpeed * Time.fixedDeltaTime);

        if (_rb.linearVelocity.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(_rb.linearVelocity.normalized, Vector3.up);
    }


    // --- 內部狀態新增變數 ---
    private Vector3 _lastHitPoint;

    void OnCollisionEnter(Collision other)
    {
        if (_exploded || !explodeOnCollision) return;
        // 檢查 LayerMask
        if (((1 << other.gameObject.layer) & collideMask) == 0) return;

        // 1. 取得碰撞點
        _lastHitPoint = other.contacts[0].point;

        // 2. 向上搜尋父物件是否實作 ISpellAffectable
        ISpellAffectable affectable = other.gameObject.GetComponentInParent<ISpellAffectable>();

        if (affectable != null && SpellManager.Instance != null)
        {
            // 3. 向管理器註冊效果
            SpellManager.Instance.RegisterEffect(affectable, spellType, _lastHitPoint);
        }

        Explode();
    }

    void OnTriggerEnter(Collider other)
    {
        if (_exploded || !explodeOnCollision) return;
        // 檢查 LayerMask
        if (((1 << other.gameObject.layer) & collideMask) == 0) return;

        // Trigger 點位通常取法術中心或使用 ClosestPoint
        _lastHitPoint = other.ClosestPoint(transform.position);

        // 2. 向上搜尋父物件是否實作 ISpellAffectable
        ISpellAffectable affectable = other.gameObject.GetComponentInParent<ISpellAffectable>();

        if (affectable != null && SpellManager.Instance != null)
        {
            // 3. 向管理器註冊效果
            SpellManager.Instance.RegisterEffect(affectable, spellType, _lastHitPoint);
        }
    
        Explode();
    }

    private void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(explodeSfx);

        // 隱藏本體與禁用物理
        if (_mesh) _mesh.enabled = false;
        if (_col) _col.enabled = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.isKinematic = true;

        // 特效切換
        if (travelVFX) travelVFX.Stop();
        
        if (hitVFX)
        {
            hitVFX.transform.SetParent(null); // 脫離父物件避免跟隨銷毀
            hitVFX.Play();
            Destroy(hitVFX.gameObject, hitVFX.main.duration + hitVFX.main.startLifetime.constantMax);
        }
        
        Destroy(gameObject, 0.05f); // 快速銷毀子彈主體
    }
}