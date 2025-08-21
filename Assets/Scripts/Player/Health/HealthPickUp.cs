// HealthPickup.cs
using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    public int healAmount = 1;
    public bool destroyOnPick = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!other.TryGetComponent<Health>(out var hp)) return;

        if (hp.GetCurrent() < hp.GetMax())
        {
            hp.Heal(healAmount);
            if (destroyOnPick) Destroy(gameObject);
        }
    }
}