using UnityEngine;
using System.Collections.Generic;

public class FXTestEffectSpawner : MonoBehaviour
{
    [Header("資源配置")]
    public GameObject effectPrefab; // 拖拽欲測試的特效或子彈進此處
    public int poolSize = 10;

    [Header("運動參數")]
    public float speed = 20f;
    public float maxDistance = 50f;
    public float shootInterval = 0.2f;

    private List<GameObject> pool;
    private List<Vector3> startPositions;
    private float timer;
    private int currentIndex = 0;

    void Start()
    {
        InitializePool();
    }

    void Update()
    {
        HandleShooting();
        UpdateProjectiles();
    }

    void InitializePool()
    {
        pool = new List<GameObject>();
        startPositions = new List<Vector3>();

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(effectPrefab);
            obj.SetActive(false);
            pool.Add(obj);
            startPositions.Add(Vector3.zero);
        }
    }

    void HandleShooting()
    {
        timer += Time.deltaTime;
        if (timer >= shootInterval)
        {
            SpawnProjectile();
            timer = 0f;
        }
    }

    void SpawnProjectile()
    {
        GameObject obj = pool[currentIndex];
        obj.transform.position = transform.position;
        obj.transform.rotation = transform.rotation;
        startPositions[currentIndex] = transform.position;
        obj.SetActive(true);

        currentIndex = (currentIndex + 1) % poolSize;
    }

    void UpdateProjectiles()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].activeSelf) continue;

            // 位移計算
            pool[i].transform.Translate(Vector3.forward * speed * Time.deltaTime);

            // 距離檢測與回收
            if (Vector3.Distance(startPositions[i], pool[i].transform.position) >= maxDistance)
            {
                pool[i].SetActive(false);
            }
        }
    }
}