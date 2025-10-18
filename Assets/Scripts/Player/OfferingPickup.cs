using UnityEngine;
using DG.Tweening;
using UnityEngine.Pool;

[RequireComponent(typeof(Collider))]
public class OfferingPickup : MonoBehaviour
{
    public CollectionSystem.CollectedType type = CollectionSystem.CollectedType.Regular;
    public int value = 1;

    [Header("Idle")]
    public float hoverAmp = 0.25f;
    public float hoverSpeed = 2f;
    public float rotateSpeed = 120f;
    [SerializeField] private float lifetimeSeconds = 8f;

    [Header("FX (detached)")]
    public ParticleSystem collectVfxPrefab;
    public AudioClip collectSfx;
    public float sfxVolume = 0.8f;

    [HideInInspector] public IObjectPool<OfferingPickup> pool;
    public bool Collected { get; private set; }

    private Vector3 _spawnPos;
    private Vector3 _originalScale;
    private float _t, _dieAt;
    private bool _attracting;
    private float _flySpeed, _arrivalDist;
    private Vector3 _target;
    private bool _needsSpawnInit;

    private Rigidbody _rb; // 可選
    [SerializeField]private Collider _col;

    void Awake()
    {
        _originalScale = transform.localScale;
        _rb = GetComponent<Rigidbody>(); // 沒有就為 null
    }

    void OnEnable()
    {
        Collected = false;
        _attracting = false;
        _t = Random.value * 100f;
        _dieAt = Time.unscaledTime + lifetimeSeconds;

        // 等 Spawner 設完 position，再在第一個 Update 抓 spawnPos
        _needsSpawnInit = true;

        var col = GetComponent<Collider>();
        col.isTrigger = true;

        // 重置剛體，避免沿用上次狀態往下掉
        if (_rb)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.useGravity = false;      // 供品通常不需要重力
            _rb.isKinematic = true;      // 讓你用 transform 移動
        }

        PlayerMagnet.Register(this);
    }

    void OnDisable()
    {
        PlayerMagnet.Unregister(this);
        transform.DOKill();
        StopAllCoroutines();
    }

    public void OnTakenFromPool()
    {
        _rb.useGravity = true;  
        _rb.isKinematic = false;   
        //transform.localScale = _originalScale;
        EnableAllRenderers(true);
    }

    public void OnReturnedToPool()
    {
        transform.localScale = _originalScale;
        transform.DOKill();
        StopAllCoroutines();
    }

    void Update()
    {
        if (_needsSpawnInit)
        {
            // 現在 Spawner 已經把 transform.position 設好
            _spawnPos = transform.position;
            _needsSpawnInit = false;
        }

        if (Collected) return;

        // Idle
        /*_t += Time.deltaTime * hoverSpeed;
        transform.position = _spawnPos + Vector3.up * Mathf.Sin(_t) * hoverAmp;
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        */

        // 吸附中
        if (_attracting)
        {
            _col.enabled = false;
            transform.position = Vector3.MoveTowards(transform.position, _target, _flySpeed * Time.deltaTime);
        }
            
        // 超時自回收
        if (Time.unscaledTime >= _dieAt)
            ReturnToPool();
    }

    public void AttractTo(Vector3 target, float speed, bool strong, float arrivalDist)
    {
        if (Collected) return;
        _target = target;
        _flySpeed = strong ? speed * 1.5f : speed;
        _arrivalDist = arrivalDist;
        _attracting = true;

        if ((transform.position - target).sqrMagnitude <= _arrivalDist * _arrivalDist)
            CollectNow();
    }

    private void CollectNow()
    {
        if (Collected) return;
        Collected = true;
        _attracting = false;

        CollectionSystem.CollectItem(type, value);

        if (collectVfxPrefab)
        {
            var vfx = Instantiate(collectVfxPrefab, transform.position, Quaternion.identity);
            vfx.Play();
            Destroy(vfx.gameObject, vfx.main.duration + vfx.main.startLifetime.constantMax + 0.1f);
        }
        if (collectSfx) AudioSource.PlayClipAtPoint(collectSfx, transform.position, sfxVolume);

        EnableAllRenderers(false);
        transform.DOKill();
        transform.DOScale(0f, 0.12f).SetEase(Ease.InBack).OnComplete(ReturnToPool);
    }

    private void ReturnToPool()
    {
        if (pool != null) pool.Release(this);
        else Destroy(gameObject);
    }

    private void EnableAllRenderers(bool on)
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = on;
    }
}
