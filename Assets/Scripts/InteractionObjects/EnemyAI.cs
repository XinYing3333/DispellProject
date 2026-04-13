using System.Collections;
using DefaultNamespace.Thought;
using DefaultNamespace.Tutorial;
using EventBus.Events.Tutorial;
using UnityEngine;
using UnityEngine.AI;
using Player.InteractionSystem;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour, ICollectable, IHitReceiver
{
    public enum AttackMode { Idle, Attack, Stun }

    [Header("General Settings")] 
    public AttackMode attackMode = AttackMode.Idle;
    
    [SerializeField, HideInInspector] 
    private string persistentId; // 手動擺放的唯一 ID

    public Transform target;
    public float detectionRange = 10f;
    public float stunTime = 2f;
    private bool _isStunned = false;
    public DamageDealer damageDealer;
    [SerializeField] private Animator _stateUIAnimator;

    private bool isPlay;
    private NavMeshAgent agent;
    
    public bool NeedCollectAnimation => true; 
    public bool IsSpellStateActive => false;
    
    private static bool isStunningBefore;
    private static bool isCollectingBefore;

    void Start()
    {
        // 檢查是否已被收集（持久存檔或本次 Session）
        if (DataManager.Instance.IsThoughtCollected(persistentId))
        {
            Destroy(gameObject);
            return;
        }

        target = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        damageDealer = GetComponent<DamageDealer>();
    }

    void Update()
    {
        if (!target || _isStunned) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget < detectionRange)
        {
            agent.isStopped = false;
            agent.destination = target.position;
            _stateUIAnimator.enabled = true;
            if (!isPlay) _stateUIAnimator.Play("enemy-warning");
            isPlay = true;
        }
        else
        {
            isPlay = false;
            agent.isStopped = true;
        }
    }

    IEnumerator OnStun()
    {
        if (!isStunningBefore)
        {
            EventBus<OnTutorialRequirementMet>.Raise(
                new OnTutorialRequirementMet { Requirement = TutorialRequirementType.FirstStunEnemy });
            isStunningBefore = true;
        }
        _isStunned = true;
        agent.isStopped = true;
        isPlay = false;
        _stateUIAnimator.Play("enemy-stunning");

        if (damageDealer) damageDealer._lockDamage = true;
        yield return new WaitForSeconds(stunTime);
        if (damageDealer) damageDealer._lockDamage = false;

        _isStunned = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Spell spell))
        {
            if(_isStunned) return;
            stunTime = 2f;
            StartCoroutine(OnStun());
        }
        if (other.CompareTag("Player"))
        {
            if(_isStunned) return;
            StartCoroutine(OnStun());
        }
    }

    public void OnHit(ThoughtPayloadSO payload)
    {
        if(_isStunned) return;
        stunTime = 3f;
        StartCoroutine(OnStun());
    }

    public void Collect()
    {
        if (!_isStunned) return;

        // 1. 記錄 ID 到 Session 暫存清單
        if (!DataManager.Instance.gameData.sessionCollectedIds.Contains(persistentId))
        {
            DataManager.Instance.gameData.sessionCollectedIds.Add(persistentId);
        }

        // 2. 增加數量（暫存至 Session）
        CollectionSystem.CollectItem(CollectionSystem.CollectedType.EnemyThough, 1);
        CollectionSystem.CollectItem(CollectionSystem.CollectedType.Though, 3);

        if (!isCollectingBefore)
        {
            EventBus<OnTutorialRequirementMet>.Raise(
                new OnTutorialRequirementMet { Requirement = TutorialRequirementType.FirstCollectEnemy });
            isCollectingBefore = true;
        }

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 自動賦予唯一 ID
        if (!Application.isPlaying && string.IsNullOrEmpty(persistentId) && !UnityEditor.EditorUtility.IsPersistent(this))
        {
            persistentId = $"Enemy_{gameObject.scene.name}_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}