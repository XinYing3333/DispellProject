using UnityEngine;

public class BouncePad : MonoBehaviour
{
    public float bounceForce = 12f;
    public float cooldownTime = 3f;
    public GameObject visual;

    private Collider padCollider;
    private bool isOnCooldown = false;
    private MeshRenderer meshRenderer;

    private void Start()
    {
        padCollider = GetComponent<Collider>();
        meshRenderer = GetComponent<MeshRenderer>();
        if (!padCollider.isTrigger) padCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isOnCooldown) return;

        if (other.CompareTag("Player"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            Animator anim = other.GetComponent<Animator>();
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, bounceForce, rb.linearVelocity.z);
                if (anim != null)
                {
                    if (anim.GetBool("Jump"))
                    {
                        anim.SetBool("Jump", false);
                        anim.SetBool("IsDoubleJump", true);
                    }

                    if (anim.GetBool("IsDoubleJump"))
                    {
                        anim.SetBool("Jump", true);
                        anim.SetBool("IsDoubleJump", false);
                    }
                    else
                    {
                        anim.SetBool("Jump", true);
                        anim.SetBool("IsDoubleJump", false);
                    }
                }
            }

            StartCoroutine(HandleCooldown());
        }
    }

    private System.Collections.IEnumerator HandleCooldown()
    {
        isOnCooldown = true;
        meshRenderer.enabled = false;
        
        if (visual != null)
            visual.SetActive(false);

        padCollider.enabled = false;
        
        yield return new WaitForSeconds(cooldownTime);

        if (visual != null)
            visual.SetActive(true);

        padCollider.enabled = true;
        isOnCooldown = false;
        meshRenderer.enabled = true;

    }
}