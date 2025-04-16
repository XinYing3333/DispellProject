using UnityEngine;

public class IncensePickup : MonoBehaviour
{
    [SerializeField] private int incenseValue = 1; // 每個香火的數值

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // 確保玩家才能收集
        {
            IncenseCollectionSystem.CollectIncense(incenseValue);
            AudioManager.Instance.PlaySFX(SFXType.PickUp);
            Destroy(gameObject); // 收集後銷毀物件
        }
    }
}