using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum AttackMode { Idle, Melee, Ranged, Patrol }

    [Header("General Settings")]
    public AttackMode attackMode = AttackMode.Idle;
    public Transform target;
    public float detectionRange = 10f;
    
    public float stunTime = 2f;
    private bool _isStunned = false;
    public Collider[] damageColliders;               // ★ 這些是會對玩家造成傷害的 Trigger/Collider（暈眩時關閉）

    private NavMeshAgent agent;
    private MeshRenderer mesh;
    private Rigidbody rb;

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        mesh = transform.GetChild(0).GetComponent<MeshRenderer>();
    }

    void Update()
    {
        if (!target) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget < detectionRange && !_isStunned)
        {
            mesh.material.color = new Color(1f , 0.2783019f, 0.2783019f);

            agent.isStopped = false;
            agent.destination = target.position;        
        }
        else if(distanceToTarget > detectionRange && !_isStunned)
        {
            mesh.material.color = new Color(0.3254717f , 0.9400993f, 1f);
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
        mesh.material.color = Color.yellow;

        if (damageColliders != null)
            foreach (var c in damageColliders) if (c) c.enabled = false;
        
        originalLayer = gameObject.layer;
        transform.tag = "Collectible";
        gameObject.layer = LayerMask.NameToLayer("Collectible");
        rb.isKinematic = false;
        
        yield return new WaitForSeconds(stunTime);

        if (damageColliders != null)
            foreach (var c in damageColliders) if (c) c.enabled = true;
        
        _isStunned = false;
        mesh.material.color = new Color(1f , 0.2783019f, 0.2783019f);
        transform.tag = "Untagged";
        gameObject.layer = originalLayer;
        rb.isKinematic = true;

    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Spell spell))
        {
            StartCoroutine(OnStun());
        }
    }
}
