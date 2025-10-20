using EventBus.Events.Health;
using Player;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KillVolume : MonoBehaviour
{
    public int damage = 1;

    private void Reset()
    {
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // 取得 Health 元件
        if (!other.TryGetComponent(out Health health)) return;

        health.ApplyDamage(new DamageInfo(damage, Vector3.back, 0,false, true));
    }
}

