using UnityEngine;
using UnityEngine.Pool;

public class OfferingPool : MonoBehaviour
{
    public static OfferingPool Instance { get; private set; }

    [SerializeField] private OfferingPickup prefab;
    [SerializeField] private int prewarm = 64;
    [SerializeField] private int maxSize = 512;

    private IObjectPool<OfferingPickup> _pool;

    void Awake()
    {
        if (Instance) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _pool = new ObjectPool<OfferingPickup>(
            createFunc: () => Instantiate(prefab),
            actionOnGet: (p) =>
            {
                p.gameObject.SetActive(true);
                p.pool = _pool;
                p.OnTakenFromPool();
            },
            actionOnRelease: (p) =>
            {
                p.OnReturnedToPool();
                p.gameObject.SetActive(false);
            },
            actionOnDestroy: (p) => Destroy(p.gameObject),
            collectionCheck: false,
            defaultCapacity: prewarm,
            maxSize: maxSize
        );

        // 預熱
        for (int i = 0; i < prewarm; i++)
        {
            var p = _pool.Get();
            _pool.Release(p);
        }
    }

    public OfferingPickup Get() => _pool.Get();
    public void Release(OfferingPickup p) => _pool.Release(p);
}