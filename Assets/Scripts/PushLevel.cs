using UnityEngine;

public class PushLevel : MonoBehaviour
{
    private bool playerInRange = false;
    private bool cloneInRange = false;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        bool isPush = playerInRange && cloneInRange;
        rb.mass = isPush ? 200f : 1000f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            playerInRange = true;

        if (collision.gameObject.CompareTag("Clone"))
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