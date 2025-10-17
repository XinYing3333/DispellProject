using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class LevelCrossRoad : MonoBehaviour
{
    [Header("目的地（對面人行道/安全島上的空物件）")] public Transform destination;
   public Transform destination2;

    [Header("抵達判定")] public float arriveDistance = 0.4f;

    [Header("動畫（可選）")] public Animator animator;
    public string walkBool = "isMove";

    private NavMeshAgent _agent;
    private bool _started;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (animator) animator.SetBool(walkBool, false);
    }

    // 讓路燈的 onFirstHit 直接呼叫這個
    public void StartCross()
    {
        if (_started || destination == null) return;
        _started = true;

        if (animator) animator.SetBool(walkBool, true);

        _agent.isStopped = false;
        _agent.updateRotation = true; // 讓代理自行面向移動方向
        _agent.SetDestination(destination.position);

        StartCoroutine(WaitToArrive());
    }
    
    public void StartCrossSecond()
    {
        if (destination2 == null) return;

        if (animator) animator.SetBool(walkBool, true);

        _agent.isStopped = false;
        _agent.updateRotation = true; // 讓代理自行面向移動方向
        _agent.SetDestination(destination2.position);

        StartCoroutine(WaitToArrive());
    }

    private IEnumerator WaitToArrive()
    {
        // 確保目標持續有效、路徑可走就前進
        while (destination != null)
        {
            if (!_agent.pathPending && _agent.remainingDistance <= arriveDistance)
                break;
            yield return null;
        }

        if (animator) animator.SetBool(walkBool, false);
        _agent.isStopped = true;
        // ★ 到達後如果要做：看向某方向/播台詞/淡出…在這裡加
    }
}
