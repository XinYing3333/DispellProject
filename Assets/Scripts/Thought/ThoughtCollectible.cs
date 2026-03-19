using Player.InteractionSystem;
using UnityEngine;

public class  ThoughtCollectible : MonoBehaviour, ICollectable
{
    private string _spawnId;
    private ThoughtPlacer _owner;

    public Animator anim;
    public string idleSubSM = "IdlePool";  // 子狀態機名稱
    public int idleCount = 2;//動畫總數，animator clips 命名排列以 0 開始
    public float minGap = 1.8f, maxGap = 4.0f;
    public float crossFade = 0.12f;
    
    float timer;
    
    public bool NeedCollectAnimation => true;
    public bool IsSpellStateActive => false;

    void Start() { timer = Random.Range(minGap, maxGap); }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            int idx = Random.Range(0, idleCount);
            anim.CrossFade($"{idleSubSM}.Idle_{idx}", crossFade, 0, 0f);
            timer = Random.Range(minGap, maxGap);
        }
    }
    
    public void Init(string spawnId, ThoughtPlacer owner)
    {
        _spawnId = spawnId;
        _owner = owner;
    }

    public void Collect()
    {
        if (LevelStateStore.Instance != null)
            LevelStateStore.Instance.MarkCollectedSession(_spawnId);

        CollectionSystem.CollectItem(CollectionSystem.CollectedType.Though, 1);
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