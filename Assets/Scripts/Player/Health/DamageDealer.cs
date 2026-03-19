// DamageDealer.cs

using System.Collections.Generic;
using EventBus.Events.Health;
using UnityEngine;
using Player;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class DamageDealer : MonoBehaviour
{
    public int damage = 1;
    public float knockbackForce = 6f;
    public bool continuous = false;          // true = 站在毒池持續掉血
    public float tickInterval = 0.5f;        // 連續傷害的間隔
    public bool useHitForward = true;        // 方向用我自己的forward
    public Transform directionRef;           // 若指定，方向從這裡取

    public bool _lockDamage = false;
    private float _nextTickTime;
    
    public UnityEvent onInvoke;


    private void OnTriggerEnter(Collider other) { TryHit(other); }
    private void OnTriggerStay(Collider other)  { if (continuous && Time.time >= _nextTickTime) TryHit(other); }

    void TryHit(Collider other)
    {
        if(_lockDamage)return;
        if (!other.CompareTag("Player")) return;
        if (!other.TryGetComponent<Health>(out var hp)) return;

        Vector3 dir;
        if (useHitForward) dir = (directionRef ? directionRef.forward : transform.forward);
        else dir = (other.transform.position - transform.position).normalized;
        var info = new DamageInfo(damage, dir, knockbackForce);
        
        onInvoke?.Invoke();
        
        hp.ApplyDamage(info);
        _nextTickTime = Time.time + tickInterval;
    }
}