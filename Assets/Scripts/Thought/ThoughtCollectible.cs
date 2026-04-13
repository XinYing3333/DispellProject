using Player.InteractionSystem;
using UnityEngine;

public class  ThoughtCollectible : MonoBehaviour, ICollectable
{
    [SerializeField, HideInInspector] 
    private string persistentId; // 固定的唯一編號
    private string _runtimeId;
    private ThoughtPlacer _owner;

    public Animator anim;
    public string idleSubSM = "IdlePool";  // 子狀態機名稱
    public int idleCount = 2;//動畫總數，animator clips 命名排列以 0 開始
    public float minGap = 1.8f, maxGap = 4.0f;
    public float crossFade = 0.12f;
    
    float timer;
    
    public bool NeedCollectAnimation => true;
    public bool IsSpellStateActive => false;

    void Start() 
    { 
        timer = Random.Range(minGap, maxGap);
        // 如果是手動擺放在場景中的物件，檢查是否已被收集
        if (_owner == null && DataManager.Instance.gameData.collectedThoughtIds.Contains(persistentId))
        {
            gameObject.SetActive(false);
        }
    }
    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            int idx = Random.Range(0, idleCount);
            if(anim) anim.CrossFade($"{idleSubSM}.Idle_{idx}", crossFade, 0, 0f);
            timer = Random.Range(minGap, maxGap);
        }
    }
    
    public void Init(string id, ThoughtPlacer owner)
    {
        _runtimeId = id;
        _owner = owner;
        // 動態生成的物件在 Init 時檢查
        if (DataManager.Instance.gameData.collectedThoughtIds.Contains(id))
        {
            if (_owner != null) _owner.ReturnThoughToPool(gameObject);
            else gameObject.SetActive(false);
        }
    }

    public void Collect()
    {
        string finalId = string.IsNullOrEmpty(_runtimeId) ? persistentId : _runtimeId;
    
        // 改為存入 DataManager 的暫存清單
        if (!DataManager.Instance.gameData.sessionCollectedIds.Contains(finalId))
        {
            DataManager.Instance.gameData.sessionCollectedIds.Add(finalId);
        }

        CollectionSystem.CollectItem(CollectionSystem.CollectedType.Though, 1);
    
        if (_owner != null) _owner.ReturnThoughToPool(gameObject);
        else gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && string.IsNullOrEmpty(persistentId) && !UnityEditor.EditorUtility.IsPersistent(this))
        {
            persistentId = $"{gameObject.scene.name}_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}