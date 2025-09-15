using UnityEngine;

public class ThoughtCollectible : MonoBehaviour
{
    private string _spawnId;
    private ThoughPlacer _owner;

    public Animator anim;
    public string idleSubSM = "IdlePool";  // 子狀態機名稱
    public int idleCount = 4;
    public float minGap = 1.8f, maxGap = 4.0f;
    public float crossFade = 0.12f;

    float timer;

    void Start() { timer = Random.Range(minGap, maxGap); }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            int idx = Random.Range(0, idleCount);
            // 直接切換，不需要在 Animator 畫線
            anim.CrossFade($"{idleSubSM}.Idle_{idx}", crossFade, 0, 0f);
            timer = Random.Range(minGap, maxGap);
        }
    }
    
    public void Init(string spawnId, ThoughPlacer owner)
    {
        _spawnId = spawnId;
        _owner = owner;
    }

    public void Collect()
    {
        LevelStateStore.Instance.MarkCollectedSession(_spawnId);
        CollectionSystem.CollectItem(CollectionSystem.CollectedType.Regular, 1);
        _owner.ReturnThoughToPool(gameObject);
    }
    
    /*private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // 念頭 → 暫存
        LevelStateStore.Instance.MarkCollectedSession(_spawnId);

        // 可選：如果念頭同時給玩家「物品」：
        CollectionSystem.CollectItem(CollectionSystem.CollectedType.Regular, 1);

        _owner.ReturnThoughToPool(gameObject);
    }*/
}