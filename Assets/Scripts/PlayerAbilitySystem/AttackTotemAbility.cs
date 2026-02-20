using UnityEngine;
using System.Linq;

namespace AbilitySystem
{
    public class AttackTotemAbility : IAbility
    {
        private Transform playerTransform;
        private float cooldown = 2.0f;
        private float lastUseTime = -999f;
        
        // 假設你的敵人都在這個 Layer 或帶有特定 Tag
        private LayerMask enemyLayer; 

        public AttackTotemAbility(Transform playerTransform, LayerMask enemyLayer)
        {
            this.playerTransform = playerTransform;
            this.enemyLayer = enemyLayer;
        }

        public void Tick() { /* 可以在這裡處理冷卻 UI 的更新 */ }

        public void Activate()
        {
            Debug.Log("裝備攻擊圖騰");
        }

        public void Deactivate()
        {
            Debug.Log("卸下攻擊圖騰");
        }

        public void Use()
        {
            if (Time.time < lastUseTime + cooldown)
            {
                Debug.Log("攻擊圖騰冷卻中...");
                return;
            }

            FindAndAttackFurthestEnemy();
            lastUseTime = Time.time;
        }

        private void FindAndAttackFurthestEnemy()
        {
            // 找出一定範圍內的敵人 (比如半徑 30 單位內)
            Collider[] hits = Physics.OverlapSphere(playerTransform.position, 30f, enemyLayer);
            
            if (hits.Length == 0)
            {
                Debug.Log("附近沒有敵人可攻擊");
                return;
            }

            // 找出距離最遠的敵人
            Transform furthestEnemy = null;
            float maxDistSq = -1f;

            foreach (var hit in hits)
            {
                // 如果你有判斷「是否在戰鬥中」的條件，可以加在這裡
                // if (!hit.GetComponent<EnemyAI>().IsInCombat) continue;

                float distSq = (hit.transform.position - playerTransform.position).sqrMagnitude;
                if (distSq > maxDistSq)
                {
                    maxDistSq = distSq;
                    furthestEnemy = hit.transform;
                }
            }

            if (furthestEnemy != null)
            {
                Debug.Log($"發射追擊彈！目標: {furthestEnemy.name}，距離: {Mathf.Sqrt(maxDistSq)}");
                
                // TODO: 在這裡生成你的追擊特效或投射物
                // 投射物打到敵人後，呼叫敵人的 Stun(較短時間) 函式
            }
        }
    }
}