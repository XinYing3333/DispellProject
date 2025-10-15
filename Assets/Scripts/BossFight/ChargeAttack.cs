using System.Collections;
using UnityEngine;

namespace BossFight
{
    public class ChargeAttack : IBossAttack
    {
        public string Id => "Charge";
        private readonly ChargeConfig cfg;
        private readonly ChargeTelegraph telegraphPrefab;
        private readonly Health playerHP;

        public ChargeAttack(ChargeConfig cfg, ChargeTelegraph telegraphPrefab, Health playerHP)
        {
            this.cfg = cfg;
            this.telegraphPrefab = telegraphPrefab;
            this.playerHP = playerHP;
        }

        public IEnumerator Execute(BossContext C)
        {
            // 升空
            yield return C.Services.MoveVerticalTo(C.ModelRoot, C.Owner.HoverHeight, C.Owner.RiseSpeed);

            // 起點/終點
            Vector3 p = C.Player.position;
            Vector2 dir2 = Random.insideUnitCircle.normalized;
            float startDist = Mathf.Lerp(C.Owner.MinLandDist, C.Owner.MaxLandDist, Random.value);

            Vector3 start = C.Services.GetGroundBelow(p + new Vector3(dir2.x,0,dir2.y)*startDist);
            Vector3 dashDir = (p - start); dashDir.y = 0f;
            if (dashDir.sqrMagnitude < 0.01f) dashDir = Vector3.right;
            dashDir.Normalize();
            Vector3 end = C.Services.GetGroundBelow(p + dashDir * cfg.distance);
            
            start += Vector3.up * C.Owner.groundOffset;
            end   += Vector3.up * C.Owner.groundOffset;

            // 前搖箭頭
            if (telegraphPrefab != null)
            {
                var tele = ChargeTelegraph.Spawn(start, end, telegraphPrefab, cfg.windup, 0.35f, 0.15f, new Color(1f,0.35f,0.2f,0.95f));
                bool done = false; tele.OnFinished += () => done = true;
                while (!done) yield return null;
            }
            else yield return new WaitForSeconds(cfg.windup);

            // 落到 start
            yield return C.Services.MoveTo(C.ModelRoot, start, C.Owner.DescendSpeed);

            // 地貼衝刺（修正版）
            bool damaged = false;
            while (Vector2.SqrMagnitude(
                       new Vector2(C.ModelRoot.position.x, C.ModelRoot.position.z) -
                       new Vector2(end.x, end.z)
                   ) > 0.05f) // ← 只比XZ距離
            {
                Vector3 cur = C.ModelRoot.position;
                Vector3 curXZ = new Vector3(cur.x, 0, cur.z);
                Vector3 endXZ = new Vector3(end.x, 0, end.z);
                Vector3 stepXZ = Vector3.MoveTowards(curXZ, endXZ, cfg.speed * Time.deltaTime);

                Vector3 next = new Vector3(stepXZ.x, cur.y, stepXZ.z);

                if (cfg.stickToGround && C.Services.TryProjectToGroundXZ(stepXZ, cfg.probeUp, cfg.probeDown, out var gp))
                {
                    float ty = gp.y + cfg.groundOffset;
                    next.y = Mathf.Lerp(cur.y, ty, 20f * Time.deltaTime);
                }

                // 一次性膠囊傷害
                if (!damaged)
                {
                    damaged = C.Services.DoCapsuleDamageOnce(
                        cur + Vector3.up * 0.5f,
                        next + Vector3.up * 0.5f,
                        cfg.width * 0.5f,
                        cfg.damage,
                        playerHP
                    );
                }

                // 面向 & 移動
                Vector3 face = next - cur; face.y = 0f;
                if (face.sqrMagnitude > 0.0001f)
                    C.ModelRoot.rotation = Quaternion.Slerp(C.ModelRoot.rotation, Quaternion.LookRotation(face), 12f * Time.deltaTime);

                C.ModelRoot.position = next;
                yield return null;
            }

            yield return new WaitForSeconds(cfg.recover);

        }
    }
}
