using UnityEngine;

public class BouncePad : MonoBehaviour
{
    [Header("Bounce Settings")]
    public float bounceForce = 12f;

    [Header("Animation")]
    public Animator padAnimator;              // 跳板自己的 Animator
    public string triggerParam = "Trigger";   // 你 Animator 內的 Trigger 參數名稱（已存在：Trigger）

    private Collider padCollider;

    private void Start()
    {
        padAnimator = GetComponent<Animator>();
        padCollider = GetComponent<Collider>();
        if (padCollider != null && !padCollider.isTrigger) padCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // 1) 上彈玩家
        var rb   = other.attachedRigidbody;
        var anim = other.GetComponent<Animator>();

        if (rb != null)
        {
            // 直接把垂直速度設為正值，確保向上彈
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, bounceForce, rb.linearVelocity.z);
            AudioManager.Instance.PlaySFX(SFXType.BouncePad);
            // 也可以改用 AddForce：
            // rb.AddForce(Vector3.up * bounceForce, ForceMode.VelocityChange);
        }

        // 2) 切玩家動畫（沿用你原本的邏輯）
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

        // 3) 觸發跳板動畫 & 上鎖
        if (padAnimator != null)
        {
            padAnimator.SetTrigger(triggerParam);
        }
    }
}
