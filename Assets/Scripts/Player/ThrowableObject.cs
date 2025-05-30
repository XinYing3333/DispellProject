using System;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class ThrowableObject : MonoBehaviour
{
    public GameObject fxPrefab;        // FX 特效的 Prefab
    private bool fxSpawned = false;    // 控制 FX 只生成一次

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
            //SpawnItem();  // 時間到後生成物件與特效
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.TryGetComponent<InteractionPoint>(out InteractionPoint interactionPoint))
        {
            //SpawnItem();  // 生成物件
            Destroy(gameObject);  // 移除自身
        }
    }

}