using UnityEngine;
using UnityEngine.AI;

public class CloneFollower : MonoBehaviour
{
    public Transform player;
    private NavMeshAgent agent;
    private Animator animator;

    public float followDistance = 2f;
    public float maxDistance = 15f;

    private bool isControlledExternally = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = transform.GetChild(0).GetComponent<Animator>();
    }

    void Update()
    {
        if (player == null || isControlledExternally) return;

        FollowPlayer();
    }

    private void StopMoving()
    {
        agent.ResetPath();
        animator.SetBool("isMove", false);
        animator.speed = 1f;
    }

    private void FollowPlayer()
    {
        float dist = Vector3.Distance(player.position, transform.position);

        if (dist > maxDistance)
        {
            StopMoving();
        }
        else if (dist > followDistance)
        {
            agent.SetDestination(player.position);
            agent.speed = dist > 8f ? 5f : 8f;
            animator.SetBool("isMove", true);
            animator.speed = dist > 8f ? 1f : 2f;
        }
        else
        {
            StopMoving();
        }
    }

    // ========== 推動控制 ==========

    public void EnableExternalControl()
    {
        isControlledExternally = true;
        StopMoving();
    }

    public void DisableExternalControl()
    {
        isControlledExternally = false;
    }

    public void MoveToAssist(Vector3 assistPosition)
    {
        EnableExternalControl();
        agent.SetDestination(assistPosition);
        animator.SetBool("isMove", true);
        animator.SetBool("isHelping", true); // 若有推動動畫
    }

    public void StopAssisting()
    {
        DisableExternalControl();
        animator.SetBool("isHelping", false);
    }
}
