using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class Spell : MonoBehaviour
{
    [Header("Type Definition")]
    public SpellType spellType; // 接收外部注入的法術屬性

    [Header("Visual / FX")]
    public GameObject travelFxPrefab;
    public GameObject smokeFxPrefab;
    public SFXType explodeSfx = SFXType.Spawn;

    [Header("Flight")]
    public bool enableHoming = false;
    public float homingSpeed = 8f;
    public float rotateSpeed = 8f;
    public float fuseLifetime = 0.35f;

    [Header("Impact")]
    public bool explodeOnCollision = true;
    public LayerMask collideMask = ~0;

    [Header("Smoke")]
    public float smokeDuration = 2.0f;
    public float cleanupDelay = 0.08f;

    // --- 內部狀態 ---
    private Rigidbody _rb;
    private Collider _col;
    private MeshRenderer _mesh;
    private GameObject _travelFxInst;

    private float _life;
    private bool _exploded;
    private Transform _target;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        _mesh = GetComponent<MeshRenderer>();
        _life = fuseLifetime;
    }

    void Start()
    {
        ApplySpellTypeSettings();

        if (travelFxPrefab)
        {
            _travelFxInst = Instantiate(travelFxPrefab, transform.position, transform.rotation, transform);
        }
    }

    // 依據外部寫入的 spellType 覆寫自身屬性與視覺表現
    private void ApplySpellTypeSettings()
    {
        if (_mesh)
        {
            switch (spellType)
            {
                // 依據實際定義的 SpellType 名稱與需求調整內部數值
                case SpellType.StopSpell:     
                    _mesh.material.color = new Color(1f, 0.8f, 0.2f); 
                    break;
                case SpellType.AttackSpell:     
                    _mesh.material.color = new Color(1f, 0.2f, 0.2f); 
                    break;
            }
        }

        // 若需針對特定 SpellType 修改導引速度或引爆音效，同步於此處指派：
        // if (spellType == SpellType.AttackSpell) homingSpeed = 12f;
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
            ExplodeAsSmoke();
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

    void OnCollisionEnter(Collision other)
    {
        if (_exploded || !explodeOnCollision) return;
        if (((1 << other.gameObject.layer) & collideMask) == 0) return;
        ExplodeAsSmoke();
    }

    void OnTriggerEnter(Collider other)
    {
        if (_exploded || !explodeOnCollision) return;
        if (((1 << other.gameObject.layer) & collideMask) == 0) return;
        ExplodeAsSmoke();
    }

    private void ExplodeAsSmoke()
    {
        if (_exploded) return;
        _exploded = true;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(explodeSfx);

        if (_mesh) _mesh.enabled = false;
        if (_col) _col.enabled = false;
        if (_travelFxInst) Destroy(_travelFxInst);
        
        _rb.linearVelocity = Vector3.zero;
        _rb.isKinematic = true;
        _rb.useGravity = false;

        GameObject fx = null;
        if (smokeFxPrefab)
        {
            fx = Instantiate(smokeFxPrefab, transform.position, Quaternion.identity);
        }

        StartCoroutine(Co_SmokeLife(fx));
    }

    private IEnumerator Co_SmokeLife(GameObject fx)
    {
        yield return new WaitForSeconds(smokeDuration);
        if (fx) Destroy(fx, cleanupDelay);
        Destroy(gameObject, cleanupDelay);
    }
}