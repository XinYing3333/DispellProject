using DefaultNamespace.Thought;
using Player.InteractionSystem;
using UnityEngine;
using UnityEngine.Serialization;

public class TrafficLightHitTarget : MonoBehaviour, IHitReceiver
{
    [Header("Refs")]
    public RoadFader road;              // 斑馬線淡入/淡出控制
    public Collider crossRoad;            // 阻擋用碰撞器（有就用；沒有就忽略）

    [Header("Timing")]
    public float fadeInTime  = 0.6f;
    public float openSeconds = 6f;      // 倒數持續時間
    public float fadeOutTime = 0.6f;

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
}