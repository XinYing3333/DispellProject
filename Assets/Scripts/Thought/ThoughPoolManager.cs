using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ThoughPoolManager : MonoBehaviour
{
    public static ThoughPoolManager Instance { get; private set; }

    [Header("Pool 設定")]
    public GameObject thoughPrefab;
    public int initialSize = 100;

    private Queue<GameObject> pool = new Queue<GameObject>();
    private Transform poolContainer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        poolContainer = new GameObject("SharedThoughPool").transform;
        poolContainer.SetParent(transform);

        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < initialSize; i++)
        {
            GameObject coin = Instantiate(thoughPrefab, poolContainer);
            coin.SetActive(false);
            pool.Enqueue(coin);
        }
    }

    public GameObject Get(Vector3 position)
    {
        GameObject coin;

        if (pool.Count > 0)
        {
            coin = pool.Dequeue();
        }
        else
        {
            coin = Instantiate(thoughPrefab, poolContainer);
        }

        coin.transform.position = position;
        coin.transform.SetParent(null);
        coin.SetActive(true);
        return coin;
    }

    public void Return(GameObject coin)
    {
        coin.SetActive(false);
        coin.transform.SetParent(poolContainer);
        pool.Enqueue(coin);
    }
}
