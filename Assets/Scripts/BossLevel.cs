using UnityEngine;

public class BossLevel : MonoBehaviour
{
    private float bossHealth = 100f;
    
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Spell spell))
        {
           
        }
    }
}
