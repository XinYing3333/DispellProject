using UnityEngine;

public class IncensePickup : MonoBehaviour
{
    [SerializeField] private int incenseValue = 1;
    [SerializeField] private float absorbDistance = 3f;
    [SerializeField] private float moveSpeed = 5f;

    private Transform player;
    private bool isAttracted = false;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance < absorbDistance)
        {
            isAttracted = true;
        }

        if (isAttracted)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isAttracted) return;
        if (other.CompareTag("Player"))
        {
            IncenseCollectionSystem.CollectIncense(incenseValue);
            AudioManager.Instance.PlaySFX(SFXType.PickUp);
            Destroy(gameObject);
        }
    }
}