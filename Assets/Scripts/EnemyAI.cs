using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Player.InteractionSystem;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour, ICollectable
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

    private NavMeshAgent agent;

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
        }
        else if (distanceToTarget > detectionRange && !_isStunned)
        {
        }
        else
        {
            agent.isStopped = true;
        }
    }

    int originalLayer;

    IEnumerator OnStun()
    {
        _isStunned = true;
        agent.isStopped = true;

        if (damageDealer) damageDealer._lockDamage = true;

        yield return new WaitForSeconds(stunTime);

        if (damageDealer) damageDealer._lockDamage = false;

        _isStunned = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Spell spell))
        {
            StartCoroutine(OnStun());
        }
    }

    public void Collect()
    {
        if (!_isStunned) return;
        //LevelStateStore.Instance.MarkCollectedSession(_spawnId);
        CollectionSystem.CollectItem(CollectionSystem.CollectedType.Regular, 1);
        Destroy(gameObject);
        //_owner.ReturnThoughToPool(gameObject);
    }
}