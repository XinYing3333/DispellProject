// ThrowingSystem.cs
using UnityEngine;

public class ThrowingSystem
{
    private GameObject throwablePrefab;
    private GameObject spellPrefab;
    private Transform throwOrigin;
    private float throwForce;

    // 新增：可選擇性導入 AimAssist
    private AimAssist aimAssist;

    public ThrowingSystem(GameObject throwablePrefab, GameObject spellPrefab, Transform throwOrigin, float throwForce, AimAssist aimAssist = null)
    {
        this.throwablePrefab = throwablePrefab;
        this.spellPrefab = spellPrefab;
        this.throwOrigin = throwOrigin;
        this.throwForce = throwForce;
        this.aimAssist = aimAssist;
    }

    public void ThrowObject(Transform player)
    {
        GameObject selectedPrefab = spellPrefab;

        GameObject go = GameObject.Instantiate(selectedPrefab, throwOrigin.position, throwOrigin.rotation);
        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (!rb)
        {
            Debug.LogError("[ThrowingSystem] Prefab 沒有 Rigidbody，無法投擲。");
            return;
        }

        // 有 AimAssist 就取它的方向；沒有就退回玩家 forward（或螢幕中心 Ray）
        Vector3 dir = (aimAssist != null) ? aimAssist.GetAimDirection()
            : (player ? player.forward : Vector3.forward);
        if (dir.sqrMagnitude < 1e-6f) dir = (player ? player.forward : Vector3.forward);

        // 讓拋射物朝向飛行方向（避免粒子/碰撞方向性錯亂）
        go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        // 用速度直射，比 AddForce(Impulse) 更可控；throwForce = 速度(m/s)
        rb.linearVelocity = dir * throwForce;

        // （可選）若子彈會撞到玩家自身，忽略一次碰撞
        // var playerCol = player.GetComponentInChildren<Collider>();
        // var bulletCol = go.GetComponentInChildren<Collider>();
        // if (playerCol && bulletCol) Physics.IgnoreCollision(playerCol, bulletCol, true);

#if UNITY_EDITOR
        if (aimAssist && aimAssist.CurrentTarget)
            Debug.Log($"[ThrowingSystem] 鎖定目標：{aimAssist.CurrentTarget.name}");
        else
            Debug.Log("[ThrowingSystem] 無鎖定目標，使用視角/前向");
#endif
    }

}