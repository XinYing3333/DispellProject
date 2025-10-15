using UnityEngine;
using System.Collections;
using Player;

namespace BossFight
{
    public class BossServices : MonoBehaviour
    {
        [Header("Layers")]
        [SerializeField] private LayerMask groundMask = ~0;

        // === 你現有的方法直接搬過來 ===
        public Vector3 GetGroundBelow(Vector3 pos)
        {
            Ray ray = new Ray(pos + Vector3.up * 30f, Vector3.down);
            if (Physics.Raycast(ray, out var hit, 60f, groundMask, QueryTriggerInteraction.Ignore))
                return hit.point;
            return pos;
        }

        public IEnumerator MoveVerticalTo(Transform t, float targetHeight, float speed)
        {
            // ✅ 把 targetHeight 解釋成「離地高度」
            if (TryProjectToGroundXZ(t.position, 5f, 10f, out var gp))
            {
                float targetY = gp.y + targetHeight; // ← 這樣才是離地高度
                while (Mathf.Abs(t.position.y - targetY) > 0.05f)
                {
                    float y = Mathf.MoveTowards(t.position.y, targetY, speed * Time.deltaTime);
                    t.position = new Vector3(t.position.x, y, t.position.z);
                    yield return null;
                }
            }
        }


        public IEnumerator MoveHorizontalTo(Transform t, Vector3 targetPos, float speed)
        {
            Vector3 dest = new Vector3(targetPos.x, t.position.y, targetPos.z);
            while (Vector3.SqrMagnitude(new Vector3(t.position.x,0,t.position.z) - new Vector3(dest.x,0,dest.z)) > 0.01f)
            {
                Vector3 cur = t.position;
                Vector3 step = Vector3.MoveTowards(new Vector3(cur.x,0,cur.z), new Vector3(dest.x,0,dest.z), speed * Time.deltaTime);
                t.position = new Vector3(step.x, cur.y, step.z);

                Vector3 faceDir = (new Vector3(dest.x, cur.y, dest.z) - cur);
                faceDir.y = 0f;
                if (faceDir.sqrMagnitude > 0.001f)
                    t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(faceDir), 10f * Time.deltaTime);

                yield return null;
            }
        }

        public IEnumerator MoveTo(Transform t, Vector3 targetPos, float speed)
        {
            while ((t.position - targetPos).sqrMagnitude > 0.0025f)
            {
                t.position = Vector3.MoveTowards(t.position, targetPos, speed * Time.deltaTime);
                yield return null;
            }
            t.position = targetPos;
        }

        // AoE/膠囊傷害：把你現有的 DoLandingAoE / DoCapsuleDamage 也搬過來，改為 public
        public void DoLandingAoE(Vector3 center, float radius, int damage, Health playerHP)
        {
            Collider[] hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
            foreach (var c in hits)
            {
                if (!c.CompareTag("Player")) continue;
                Vector3 dir = c.transform.position - center;
                var knockbackForce = 2f;
                var info = new DamageInfo(damage, dir, knockbackForce);
                playerHP?.ApplyDamage(info);
            }
        }

        public bool DoCapsuleDamageOnce(Vector3 a, Vector3 b, float radius, int damage, Health playerHP)
        {
            Collider[] hits = Physics.OverlapCapsule(a, b, radius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].CompareTag("Player"))
                {
                    Vector3 dir = (hits[i].transform.position - a).normalized;
                    var info = new DamageInfo(damage, dir, 2f);
                    playerHP?.ApplyDamage(info);
                    return true; // 已命中一次
                }
            }
            return false;
        }

        // 需要的話也把「貼地投射」「GroundHug Dash」封裝進來
        public bool TryProjectToGroundXZ(Vector3 xz, float up, float down, out Vector3 groundPoint)
        {
            Vector3 origin = new Vector3(xz.x, xz.y + up, xz.z);
            if (Physics.Raycast(origin, Vector3.down, out var hit, up + down, groundMask, QueryTriggerInteraction.Ignore))
            {
                groundPoint = hit.point;
                return true;
            }
            groundPoint = xz; return false;
        }
    }
}
