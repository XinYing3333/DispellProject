using DefaultNamespace.Thought;
using Player.InteractionSystem;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class TrafficLightHitTarget : MonoBehaviour, IHitReceiver
{
    [Header("Refs")]
    public RoadFader road;              // 斑馬線淡入/淡出控制
    public Collider crossRoad;            // 阻擋用碰撞器（有就用；沒有就忽略）

    [Header("Timing")]
    public float fadeInTime  = 1f;
    public float openSeconds = 6f;      // 倒數持續時間
    public float fadeOutTime = 1f;
    [Header("OnceEvent")]
    public UnityEvent onFirstHit;

    private bool _consumed;
    
    [Header("Options")]
    public bool oneAtATime = true;      // 防止重入
    private bool _busy;

    public void OnHit(ThoughtPayloadSO payload)
    {
        if (oneAtATime && _busy) return;
        StartCoroutine(RunCycle());
    }

    private System.Collections.IEnumerator RunCycle()
    {
        _busy = true;

        // 1) 漸入顯示
        if (road) yield return road.FadeIn(fadeInTime);

        if (!_consumed) NotifyHit();
        
        // 2) 開路（例如移除阻擋）
        if (crossRoad) crossRoad.enabled = true;

        // 3) 倒數等待
        yield return new WaitForSeconds(openSeconds);

        // 4) 關路（恢復阻擋）
        if (crossRoad) crossRoad.enabled = false;

        // 5) 漸隱
        if (road) yield return road.FadeOut(fadeOutTime);

        _busy = false;
    }

    // 你既有的「被擊中」點進來呼叫這個
    private void NotifyHit()
    {
        if (_consumed) return;
        _consumed = true;
        onFirstHit?.Invoke();
        // 之後如果還要開關道路的計時，照舊在其他腳本處理
    }
}