using System.Collections;
using DefaultNamespace.Thought;
using UnityEngine;
using UnityEngine.AI;
using Player.InteractionSystem;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour, ICollectable, IHitReceiver
{
    public enum AttackMode
    {
        Idle,
        Attack,
        Stun
    }

    [Header("General Settings")] public AttackMode attackMode = AttackMode.Idle;
    public Transform target;
    public float detectionRange = 10f;

    public float stunTime = 2f;
    private bool _isStunned = false;
    public DamageDealer damageDealer;
    [SerializeField] private Animator _stateUIAnimator;

    private bool isPlay;
    private NavMeshAgent agent;
    
    public bool NeedCollectAnimation => true; 

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        damageDealer = GetComponent<DamageDealer>();

    }

    void Update()
    {
        if (!target) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget < detectionRange && !_isStunned)
        {
            agent.isStopped = false;
            agent.destination = target.position;
            _stateUIAnimator.enabled = true;
            if (!isPlay) _stateUIAnimator.Play("enemy-warning");
            isPlay = true;
        }
        else if (distanceToTarget > detectionRange && !_isStunned)
        {
        }
        else
        {
            isPlay = false;
            //_stateUIAnimator.Play("enemy-none");
            agent.isStopped = true;
        }
    }

    int originalLayer;

    public void AttackStun()
    {
        StartCoroutine(OnStun());
    }

    IEnumerator OnStun()
    {
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
            if(_isStunned)return;
            stunTime = 2f;
            StartCoroutine(OnStun());
        }
    }

    public void OnHit(ThoughtPayloadSO payload)
    {
        if(_isStunned)return;
        stunTime = 3f;
        StartCoroutine(OnStun());
    }

    public void Collect()
    {
        if (!_isStunned) return;
        //LevelStateStore.Instance.MarkCollectedSession(_spawnId);
        CollectionSystem.CollectItem(CollectionSystem.CollectedType.EnemyThough, 1);
        Destroy(gameObject);
        //_owner.ReturnThoughToPool(gameObject);
    }

}