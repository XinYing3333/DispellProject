using UnityEngine;

public class PotMagnetAttachable : MagnetAttachable
{
    [Header("碎裂資源")]
    public GameObject fracturedPrefab;    // 破碎模型
    public GameObject thoughtPrefab;      // 念頭物件
    public GameObject breakParticleFX;    // 粒子特效
    public ParticleSystem sparksParticleFX;    // 粒子特效

    [Header("判定設定")]
    public float breakForceThreshold = 8f; 
    public LayerMask wallLayer;            
    public float despawnDelay = 3f;       // 碎片消失時間

    private bool _isFlying = false;

    public override void OnMagnetAttached(Transform parent)
    {
        base.OnMagnetAttached(parent);
        _isFlying = false; // 被吸附時重置飛行狀態
    }

    public override void OnBeforeThrow()
    {
        base.OnBeforeThrow();
        _isFlying = true;  // 進入投擲飛行狀態，準備偵測碰撞
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!_isFlying) return;

        // 判斷撞擊力道與層級
        if (collision.relativeVelocity.magnitude > breakForceThreshold)
        {
            if (((1 << collision.gameObject.layer) & wallLayer) != 0)
            {
                ExecuteBreak(collision);
            }
        }
    }

    private void ExecuteBreak(Collision collision)
    {
        Vector3 hitPoint = collision.contacts[0].point;
        Vector3 hitNormal = collision.contacts[0].normal;
        sparksParticleFX.Stop();
        // 1. 生成粒子特效 (朝向法線噴發)
        if (breakParticleFX)
        {
            GameObject fx = Instantiate(breakParticleFX, hitPoint, Quaternion.LookRotation(hitNormal));
            Destroy(fx, 2f);
        }

        // 2. 生成碎裂模型
        if (fracturedPrefab)
        {
            GameObject fractured = Instantiate(fracturedPrefab, transform.position, transform.rotation);
            // 對碎片施加物理力
            foreach (Rigidbody childRb in fractured.GetComponentsInChildren<Rigidbody>())
            {
                childRb.AddExplosionForce(300f, hitPoint, 1.5f);
                Destroy(childRb.gameObject, despawnDelay + Random.Range(0f, 1f));
            }
            Destroy(fractured, despawnDelay + 1f);
        }

        // 3. 生成念頭 (稍微遠離牆面避免穿模)
        if (thoughtPrefab)
        {
            Instantiate(thoughtPrefab, hitPoint + hitNormal * 0.5f, Quaternion.identity);
        }

        // 4. 銷毀原始陶罐
        Destroy(gameObject);
    }
}