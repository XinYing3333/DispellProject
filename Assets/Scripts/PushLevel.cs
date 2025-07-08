using UnityEngine;

public class PushLevel : MonoBehaviour
{
    private bool playerInRange = false;
    private bool cloneInRange = false;
    private Rigidbody rb;
    private GameObject clone;
    
    void Start()
    {
        clone = GameObject.FindWithTag("Clone");
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!clone.activeSelf)
        {
            cloneInRange = false;
        }
        bool isPush = playerInRange && cloneInRange;
        rb.mass = isPush ? 150f : 2000f;
    }

    private void OnTriggerStay(Collider collision)
    {
        if (collision.CompareTag("Player"))
            playerInRange = true;

        if (collision.CompareTag("Clone"))
            cloneInRange = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            playerInRange = false;

        if (collision.gameObject.CompareTag("Clone"))
            cloneInRange = false;
    }
}