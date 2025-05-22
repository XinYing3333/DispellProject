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
    public float detectionRange = 2f;
    
    public float stunTime = 2f;
    private bool _isStunned = false;

    private ThoughtObject thoughtScript;
    private NavMeshAgent agent;
    private MeshRenderer mesh;

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        thoughtScript = GetComponent<ThoughtObject>();
        mesh = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        if (target == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget < detectionRange && !_isStunned)
        {
            agent.isStopped = false;
            agent.destination = target.position;        }
        else
        {
            agent.isStopped = true;
        }
    }

    IEnumerator OnStun()
    {
        _isStunned = true;
        agent.isStopped = true;
        thoughtScript.enabled = true;
        Debug.Log("Stunned");
        mesh.material.color = Color.yellow;
        transform.tag = "Collectible";
        
        yield return new WaitForSeconds(stunTime);
        
        thoughtScript.enabled = false;
        _isStunned = false;
        mesh.material.color = new Color(0.7937579f, 0f, 1f);
        transform.tag = "Untagged";

    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Spell spell))
        {
            StartCoroutine(OnStun());
        }
    }
}
