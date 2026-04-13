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

    void Start() { timer = Random.Range(minGap, maxGap); }

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
    }

    public void Collect()
    {
        CollectionSystem.CollectItem(CollectionSystem.CollectedType.Though, 1);
        
        if (_owner != null)
            _owner.ReturnThoughToPool(gameObject);
        else
            gameObject.SetActive(false); // 若非 Placer 生成則直接關閉
        
        _runtimeId = null;
    }

    
#if UNITY_EDITOR
    private void OnValidate()
    {
        // 僅在物件位於場景中且沒有 ID 時生成（排除 Prefab 資源本身）
        if (!Application.isPlaying && string.IsNullOrEmpty(persistentId) && !UnityEditor.EditorUtility.IsPersistent(this))
        {
            persistentId = $"{gameObject.scene.name}_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}