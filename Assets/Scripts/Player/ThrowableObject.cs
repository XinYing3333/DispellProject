using System;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class ThrowableObject : MonoBehaviour
{
    public GameObject fxPrefab;        // FX 特效的 Prefab
    private bool fxSpawned = false;    // 控制 FX 只生成一次

    private SpawnType spawnType;
    private float lifeTime = 1f; 
    private float counter;
    private bool hasSpawned = false;

    void Start()
    {
        counter = lifeTime;
    }

    void Update()
    {
        counter -= Time.deltaTime;  // 減少計時器
        if (counter <= 0)
        {
            SpawnItem();  // 時間到後生成物件與特效
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.TryGetComponent<InteractionPoint>(out InteractionPoint interactionPoint))
        {
            SpawnItem();  // 生成物件
            Destroy(gameObject);  // 移除自身
        }
    }

    void SpawnItem()
    {
        if (hasSpawned) return;  // 確保只執行一次
        hasSpawned = true;

        spawnType = PlayerCollector.CurrentSpawnType;
        Quaternion rotation = PlayerCollector.CurrentSpawnRotation;

        // 生成目標物件
        SpawnFactory.CreateSpawnObject(spawnType, transform.position, rotation);

        // 生成 FX 特效（確保只生成一次）
        if (!fxSpawned && fxPrefab != null)
        {
            Instantiate(fxPrefab, transform.position, Quaternion.identity);
            gameObject.GetComponent<MeshRenderer>().enabled = false;
            fxSpawned = true;  // 設定 FX 已生成
        }

        Destroy(gameObject, 1.5f);  // 投擲物消失
    }

}