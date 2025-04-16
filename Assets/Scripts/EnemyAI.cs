using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum AttackMode { Idle, Melee, Ranged, Patrol }

    [Header("General Settings")]
    public AttackMode attackMode = AttackMode.Idle;
    public Transform target;
    public float moveSpeed = 3.0f;
    public float detectionRange = 10f;

    [Header("Melee Attack Settings")]
    public float attackRange = 2f;
    public float attackDamage = 10f;
    public float attackInterval = 1.5f;

    [Header("Ranged Attack Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 10f;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    private int currentPatrolIndex = 0;
    private bool isPatrolling = false;

    private NavMeshAgent agent;
    private float lastAttackTime;

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        if (attackMode == AttackMode.Patrol)
        {
            StartCoroutine(Patrol());
        }
    }

    void Update()
    {
        if (target == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        switch (attackMode)
        {
            case AttackMode.Melee:
                HandleMeleeAttack(distanceToTarget);
                break;
            case AttackMode.Ranged:
                HandleRangedAttack(distanceToTarget);
                break;
        }
    }

    private void HandleMeleeAttack(float distanceToTarget)
    {
        if (distanceToTarget <= attackRange && Time.time - lastAttackTime > attackInterval)
        {
            lastAttackTime = Time.time;
            Attack();
        }
        else
        {
            agent.SetDestination(target.position);
        }
    }

    private void HandleRangedAttack(float distanceToTarget)
    {
        if (distanceToTarget <= detectionRange && Time.time - lastAttackTime > attackInterval)
        {
            lastAttackTime = Time.time;
            ShootProjectile();
        }
    }

    private void Attack()
    {
        Debug.Log($"{gameObject.name} 執行近戰攻擊！");
        // 在這裡可以播放攻擊動畫、傷害計算
    }

    private void ShootProjectile()
    {
        Debug.Log($"{gameObject.name} 發射子彈！");
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        rb.linearVelocity = (target.position - firePoint.position).normalized * projectileSpeed;
    }

    private IEnumerator Patrol()
    {
        isPatrolling = true;
        while (attackMode == AttackMode.Patrol)
        {
            if (patrolPoints.Length > 0)
            {
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                while (agent.pathPending || agent.remainingDistance > 0.5f)
                {
                    yield return null;
                }
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
            yield return new WaitForSeconds(2f);
        }
        isPatrolling = false;
    }
}
