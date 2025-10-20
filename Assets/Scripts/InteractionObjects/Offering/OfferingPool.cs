using UnityEngine;
using UnityEngine.Pool;

public class OfferingPool : MonoBehaviour
{
    public static OfferingPool Instance { get; private set; }

    [Header("Prefab & Pool")]
    [SerializeField] private OfferingPickup prefab;
    [SerializeField] private int prewarm = 64;
    [SerializeField] private int maxSize = 512;

    [Header("Container (auto-created)")]
    [SerializeField] private string containerName = "OfferingPool_Container";

    private IObjectPool<OfferingPickup> _pool;
    private Transform _container; // ← 所有實例都放這裡

    void Awake()
    {
        if (Instance) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureContainer();

        _pool = new ObjectPool<OfferingPickup>(
            createFunc: () =>
            {
                // 生成時就放在 container 底下，預設不啟用，避免 OnEnable 連動
                var inst = Instantiate(prefab, _container);
                inst.gameObject.SetActive(false);
                return inst;
            },
            actionOnGet: (p) =>
            {
                // 取出：確保父物件正確，然後啟用（保留你原本流程）
                if (p.transform.parent != _container)
                    p.transform.SetParent(_container, true);

                p.pool = _pool;
                p.OnTakenFromPool();
                p.gameObject.SetActive(true); // ⚠ 若你要避免啟用順序問題，可改到 Spawn API 再啟用
            },
            actionOnRelease: (p) =>
            {
                // 回收：先做清理，再放回 container 並關閉
                p.OnReturnedToPool();

                if (p.transform.parent != _container)
                    p.transform.SetParent(_container, true);

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

    /// <summary>確保存在一個乾淨的容器節點，縮放=1，避免父節點縮放影響實例。</summary>
    private void EnsureContainer()
    {
        // 儘量用已存在的同名子物件，避免重複建立
        var t = transform.Find(containerName);
        if (t == null)
        {
            var go = new GameObject(containerName);
            go.transform.SetParent(transform, false);
            _container = go.transform;
        }
        else
        {
            _container = t;
        }

        // 保證容器本身不會帶縮放/旋轉
        _container.localPosition = Vector3.zero;
        _container.localRotation = Quaternion.identity;
        _container.localScale = Vector3.one;
    }

    public OfferingPickup Get() => _pool.Get();
    public void Release(OfferingPickup p) => _pool.Release(p);

    /// <summary>
    /// （可選）提供一個便利方法，先定位再啟用，減少 OnEnable 先跑造成的位置競態。
    /// 若你改用這個方法，記得把 actionOnGet 的 SetActive(true) 拿掉，改由這裡啟用。
    /// </summary>
    public OfferingPickup SpawnAt(Vector3 position, Quaternion rotation, CollectionSystem.CollectedType type, int value)
    {
        var p = _pool.Get();
        // 若 actionOnGet 已 SetActive(true) 則此時物件已啟用；想避免的話把那行移到這裡
        p.transform.SetPositionAndRotation(position, rotation);
        p.type = type;
        p.value = value;
        return p;
    }

    /// <summary>公開 container，必要時可在 Editor 下手動檢查。</summary>
    public Transform Container => _container;
}
