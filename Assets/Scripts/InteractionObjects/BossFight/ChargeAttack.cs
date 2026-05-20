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

        // 追蹤當前的預警物件
        private ChargeTelegraph _activeTelegraph;

        public ChargeAttack(ChargeConfig cfg, ChargeTelegraph telegraphPrefab, Health playerHP)
        {
            this.cfg = cfg;
            this.telegraphPrefab = telegraphPrefab;
            this.playerHP = playerHP;
        }

        public IEnumerator Execute(BossContext C)
        {
            Vector3 playerGround = C.Services.GetGroundBelow(C.Player.position);
            float targetLandHeight = playerGround.y + C.Owner.groundOffset; 

            Vector3 startDir = (C.ModelRoot.position - C.Player.position).normalized;
            Vector3 startPos = playerGround + startDir * C.Owner.MinLandDist;
            startPos.y = targetLandHeight; 

            yield return C.Services.MoveHorizontalTo(C.ModelRoot, startPos + Vector3.up * C.Owner.HoverHeight, 35f);
            
            C.Anim.Play("bird-glide-ani"); 
            yield return C.Services.MoveTo(C.ModelRoot, startPos, C.Owner.DescendSpeed * 1.5f);

            float timer = 0f;
            Vector3 lockOnDir = C.ModelRoot.forward;
            
            // 生成並追蹤 Telegraph
            if (telegraphPrefab != null) {
                _activeTelegraph = Object.Instantiate(telegraphPrefab);
                _activeTelegraph.Setup(C.ModelRoot.position, C.ModelRoot.position, cfg.windup, 0.4f, 0.4f, new Color(1f, 0f, 0f, 0.8f));
            }

            while (timer < cfg.windup)
            {
                C.ModelRoot.position = new Vector3(C.ModelRoot.position.x, targetLandHeight, C.ModelRoot.position.z);

                Vector3 toPlayer = (C.Player.position - C.ModelRoot.position);
                toPlayer.y = 0;
                if (toPlayer.sqrMagnitude > 0.1f)
                {
                    lockOnDir = toPlayer.normalized;
                    C.ModelRoot.rotation = Quaternion.Slerp(C.ModelRoot.rotation, Quaternion.LookRotation(lockOnDir), 12f * Time.deltaTime);
                }

                if (_activeTelegraph != null) 
                    _activeTelegraph.UpdatePoints(C.ModelRoot.position, C.ModelRoot.position + lockOnDir * cfg.distance);

                timer += Time.deltaTime;
                yield return null;
            }

            // 衝刺前正常銷毀 Telegraph
            if (_activeTelegraph != null) 
            {
                Object.Destroy(_activeTelegraph.gameObject);
                _activeTelegraph = null;
            }

            Vector3 dashEndPos = C.ModelRoot.position + lockOnDir * cfg.distance;
            dashEndPos.y = targetLandHeight; 
            
            if (cfg.chargeVFXPrefab) cfg.chargeVFXPrefab.Play();
            C.Anim.Play("bird-dash-ani"); 
            bool damaged = false;

            while (Vector3.ProjectOnPlane(C.ModelRoot.position - dashEndPos, Vector3.up).sqrMagnitude > 0.1f)
            {
                Vector3 cur = C.ModelRoot.position;
                Vector3 nextXZ = Vector3.MoveTowards(new Vector3(cur.x, 0, cur.z), new Vector3(dashEndPos.x, 0, dashEndPos.z), cfg.speed * Time.deltaTime);
                
                Vector3 nextPos = new Vector3(nextXZ.x, targetLandHeight, nextXZ.z);

                if (cfg.stickToGround && C.Services.TryProjectToGroundXZ(nextPos, 2f, 2f, out var gp))
                {
                    nextPos.y = Mathf.MoveTowards(cur.y, gp.y + C.Owner.groundOffset, 20f * Time.deltaTime);
                }

                if (!damaged) damaged = C.Services.DoCapsuleDamageOnce(cur, nextPos, cfg.width, cfg.damage, playerHP);

                C.ModelRoot.position = nextPos;
                yield return null;
            }

            if (cfg.chargeVFXPrefab) cfg.chargeVFXPrefab.Stop();
            C.Anim.Play("birld-ani-stun");
            
            yield return new WaitForSeconds(cfg.recover);
            C.Anim.Play("bird-fly-ani");
            yield return C.Services.MoveVerticalTo(C.ModelRoot, C.ModelRoot.position.y + C.Owner.HoverHeight, C.Owner.RiseSpeed);
        }

        // 供外部強制中斷並清理殘留物件的方法
        public void Interrupt()
        {
            if (_activeTelegraph != null)
            {
                Object.Destroy(_activeTelegraph.gameObject);
                _activeTelegraph = null;
            }
        }
    }
}