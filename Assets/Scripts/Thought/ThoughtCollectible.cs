using UnityEngine;

public class ThoughtCollectible : MonoBehaviour
{
    private string _spawnId;
    private ThoughPlacer _owner;

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