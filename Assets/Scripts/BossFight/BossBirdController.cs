using System;
using UnityEngine;
using System.Collections;
using Player;
using Random = UnityEngine.Random;


namespace BossFight
{
    public class BossBirdController : MonoBehaviour
    {
        public enum BossState
        {
            Idle,
            LandingAttack,
            Stunned /*, Phase2, Dead*/
        }

        // 兩種攻擊型別（Phase1）
        private enum AttackPattern
        {
            Landing,
            Charge
        }

        [Header("Phase1 Pattern Select")] [SerializeField]
        private bool noImmediateRepeat = true; // 避免連續同一招

        [SerializeField] private bool useWeightedRandom = true; // 權重隨機（否則輪替）
        [SerializeField] private float weightLanding = 1.0f;
        [SerializeField] private float weightCharge = 1.0f;

        private AttackPattern _lastPattern = (AttackPattern)(-1);


        [Header("Refs")] [SerializeField] private Transform modelRoot; // 巨鳥外觀（用來移動/播放動畫）
        [SerializeField] private Animator anim; // 可選
        [SerializeField] private Transform player; // 指向玩家
        [SerializeField] private LayerMask groundMask = ~0; // 地面圖層
        [SerializeField] private LandingTelegraph telegraphPrefab;
        [SerializeField] private ChargeTelegraph chargeTelegraphPrefab; // ⬅️ 拖入剛做好的 Prefab


        [Header("Landing Settings")] [SerializeField]
        private float telegraphTime = 0.8f;

        [SerializeField] private float telegraphStartRadius = 2.6f;
        [SerializeField] private float telegraphEndRadius = 0.8f;
        [SerializeField] private float hoverHeight = 35f; // 降落前的空中高度
        [SerializeField] private float riseSpeed = 20f; // 升空速度
        [SerializeField] private float descendSpeed = 22f; // 降落速度
        [SerializeField] private float minLandDistFromPlayer = 4f;
        [SerializeField] private float maxLandDistFromPlayer = 9f;

        [Header("Post-Land Window")] [SerializeField]
        private float stunDuration = 1.2f; // 硬直/攻擊窗

        [SerializeField] private float landAoERadius = 2.6f;
        [SerializeField] private int landAoEDamage = 1;

        [Header("Loop / Debug")] [SerializeField]
        private bool autoLoopLanding = true; // 原型：自動連續出降落招

        [SerializeField] private float landingIdleBetween = 0f;
        [SerializeField] private float chargeIdleBetween = 1.5f;

        [Header("Charge Settings")] [SerializeField]
        private float chargeWindup = 0.45f; // 衝刺前搖（給玩家反應）

        [SerializeField] private float chargeDistance = 14f; // 衝刺距離（會從起點穿過玩家方向）
        [SerializeField] private float chargeSpeed = 28f; // 衝刺速度
        [SerializeField] private float chargeWidth = 1.6f; // 傷害膠囊半徑（寬）
        [SerializeField] private int chargeDamage = 1; // 傷害
        [SerializeField] private float chargeRecover = 0.6f; // 結束後僵直/冷卻

        private int landingCount = 0;   // 計算已經用了幾次 Landing
        
        private BossState _state = BossState.Idle;
        private bool _doLandingOnce;

        private Health _playerHP;

        private void Start()
        {
            _playerHP = GameObject.FindGameObjectWithTag("Player").GetComponent<Health>();
            if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
            // 原型：開場就跑降落循環
            if (autoLoopLanding) StartCoroutine(LandingLoop());
        }

        private IEnumerator LandingLoop()
        {
            while (true)
            {
                if (landingCount < 2)
                {
                    yield return StartCoroutine(DoLandingAttackRoutine());
                    landingCount++;
                    yield return new WaitForSeconds(landingIdleBetween);
                }
                else
                {
                    yield return StartCoroutine(DoChargeAttackRoutine());
                    landingCount = 0; // 重置，重新循環
                    yield return new WaitForSeconds(chargeIdleBetween);
                }
            }
        }
        
        //隨機切換攻擊狀態
        /*private IEnumerator LandingLoop()
        {
            while (true)
            {
                var next = SelectNextPattern();

                switch (next)
                {
                    case AttackPattern.Landing:
                        yield return StartCoroutine(DoLandingAttackRoutine());
                        break;
                    case AttackPattern.Charge:
                        yield return StartCoroutine(DoChargeAttackRoutine()); // ⬅️ 新增的協程，見下方
                        break;
                }

                _lastPattern = next;
                yield return new WaitForSeconds(idleBetween);
            }
        }

        private AttackPattern SelectNextPattern()
        {
            // 只有一種就直接回
            if (weightLanding <= 0f && weightCharge > 0f) return AttackPattern.Charge;
            if (weightCharge <= 0f && weightLanding > 0f) return AttackPattern.Landing;

            if (!useWeightedRandom)
            {
                // 輪替（避免連續同一招）
                if (noImmediateRepeat && _lastPattern != (AttackPattern)(-1))
                    return _lastPattern == AttackPattern.Landing ? AttackPattern.Charge : AttackPattern.Landing;

                // 第一次或允許重覆：預設先 Landing
                return _lastPattern == (AttackPattern)(-1)
                    ? AttackPattern.Landing
                    : (_lastPattern == AttackPattern.Landing ? AttackPattern.Charge : AttackPattern.Landing);
            }

            // 權重隨機 + 可避免連續
            float l = (_lastPattern == AttackPattern.Landing && noImmediateRepeat) ? 0f : Mathf.Max(0f, weightLanding);
            float c = (_lastPattern == AttackPattern.Charge && noImmediateRepeat) ? 0f : Mathf.Max(0f, weightCharge);
            float sum = l + c;
            if (sum <= 0f) return AttackPattern.Landing; // 回退

            float r = Random.value * sum;
            return (r < l) ? AttackPattern.Landing : AttackPattern.Charge;
        }
        */

        public IEnumerator DoLandingAttackRoutine()
        {
            _state = BossState.LandingAttack;

            // 1) 升空（若尚未到 hover 高度）
            yield return StartCoroutine(MoveVerticalTo(modelRoot, hoverHeight, riseSpeed));

            // 2) 選擇落點（靠近玩家、落在地面）
            Vector3 landPoint;
            landPoint = GetGroundBelow(player.position);

            // 3) 地面產生紅圈（縮小倒數）
            bool telegraphDone = false;
            var tele = LandingTelegraph.Spawn(landPoint, telegraphPrefab,
                telegraphTime, telegraphStartRadius, telegraphEndRadius);
            tele.OnTelegraphFinished += () => telegraphDone = true;

            // 可選：播放「準備降落」動畫
            //if (anim) anim.SetTrigger("PreDive");

            // 等待紅圈結束
            while (!telegraphDone) yield return null;

            // 4) 快速向落點上方對齊（在空中水平移動 + 旋轉朝向）
            yield return StartCoroutine(MoveHorizontalTo(modelRoot, landPoint + Vector3.up * hoverHeight, 20f));

            // 5) 直線降落
            //if (anim) anim.SetTrigger("Dive");
            yield return StartCoroutine(MoveTo(modelRoot, landPoint, descendSpeed));

            // 6) 落地 AoE 傷害 + 硬直（攻擊窗）
            DoLandingAoE(landPoint, landAoERadius, landAoEDamage);
            //if (anim) anim.SetTrigger("LandImpact");
            _state = BossState.Stunned;
            yield return new WaitForSeconds(stunDuration);

            // 7) 回到 Idle，留給外部決定下一招（此原型自動 loop）
            _state = BossState.Idle;
        }

        private Vector3 GetGroundBelow(Vector3 pos)
        {
            Ray ray = new Ray(pos + Vector3.up * 30f, Vector3.down);
            if (Physics.Raycast(ray, out var hit, 60f, groundMask, QueryTriggerInteraction.Ignore))
                return hit.point;
            return pos; // 沒打到就原點
        }

        private IEnumerator MoveVerticalTo(Transform t, float targetY, float speed)
        {
            Vector3 pos = t.position;
            while (Mathf.Abs(pos.y - targetY) > 0.05f)
            {
                pos = t.position;
                float y = Mathf.MoveTowards(pos.y, targetY, speed * Time.deltaTime);
                t.position = new Vector3(pos.x, y, pos.z);
                yield return null;
            }
        }

        private IEnumerator MoveHorizontalTo(Transform t, Vector3 targetPos, float speed)
        {
            // 僅改 XZ（保持高度）
            Vector3 dest = new Vector3(targetPos.x, t.position.y, targetPos.z);
            while (Vector3.SqrMagnitude(new Vector3(t.position.x, 0, t.position.z) - new Vector3(dest.x, 0, dest.z)) >
                   0.01f)
            {
                Vector3 cur = t.position;
                Vector3 step = Vector3.MoveTowards(new Vector3(cur.x, 0, cur.z), new Vector3(dest.x, 0, dest.z),
                    speed * Time.deltaTime);
                t.position = new Vector3(step.x, cur.y, step.z);

                // 面向玩家（或移動方向）
                Vector3 faceDir = (new Vector3(dest.x, cur.y, dest.z) - cur);
                faceDir.y = 0f;
                if (faceDir.sqrMagnitude > 0.001f)
                    t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(faceDir), 10f * Time.deltaTime);

                yield return null;
            }
        }

        private IEnumerator MoveTo(Transform t, Vector3 targetPos, float speed)
        {
            while ((t.position - targetPos).sqrMagnitude > 0.0025f)
            {
                t.position = Vector3.MoveTowards(t.position, targetPos, speed * Time.deltaTime);
                yield return null;
            }

            t.position = targetPos;
        }

        private void DoLandingAoE(Vector3 center, float radius, int damage)
        {
            // 原型：簡單 Overlap，打到玩家就呼叫一個受擊接口（你可以換成自己的 Health 系統/事件）
            Collider[] hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
            foreach (var c in hits)
            {
                if (c.CompareTag("Player"))
                {
                    // 假設玩家有 IDamageable
                    Vector3 dir = c.transform.position - transform.position;
                    var knockbackForce = 2f;
                    var info = new DamageInfo(damage, dir, knockbackForce);
                    if (_playerHP != null) _playerHP.ApplyDamage(info);
                    // 沒有的話你也可以用 SendMessage 或 EventBus 推事件
                }
            }

            // TODO: 之後加落地特效 / 地裂 / 攝
            //  影機震動（Cinemachine Impulse）
            // TODO: UI 顯示提示「趁現在攻擊！」
            DebugDrawCircle(center, radius, 0.3f);
        }

        private void DebugDrawCircle(Vector3 center, float radius, float time)
        {
            int seg = 40;
            Vector3 prev = center + new Vector3(radius, 0, 0);
            for (int i = 1; i <= seg; i++)
            {
                float ang = i * Mathf.PI * 2f / seg;
                Vector3 cur = center + new Vector3(Mathf.Cos(ang) * radius, 0, Mathf.Sin(ang) * radius);
                Debug.DrawLine(prev + Vector3.up * 0.05f, cur + Vector3.up * 0.05f, Color.red, time);
                prev = cur;
            }
        }

        private IEnumerator DoChargeAttackRoutine()
        {
            _state = BossState.LandingAttack; // Phase1 共用狀態旗標（也可新開 BossState.ChargeAttack）

            // 1) 升到 hover（保持與降落一致的高度語彙）
            yield return StartCoroutine(MoveVerticalTo(modelRoot, hoverHeight, riseSpeed));

            // 2) 決定「起點」與「終點」
            // 起點：在玩家外圍環（min~max）上挑一點；終點：從玩家反方向延長 chargeDistance
            Vector3 p = player.position;
            Vector2 dir2 = Random.insideUnitCircle.normalized; // 隨機一個方位（你也可用朝向玩家移動方向）
            float startDist = Mathf.Lerp(minLandDistFromPlayer, maxLandDistFromPlayer, Random.value);

            Vector3 start = GetGroundBelow(p + new Vector3(dir2.x, 0f, dir2.y) * startDist);
            Vector3 dashDir = (p - start);
            dashDir.y = 0f;
            if (dashDir.sqrMagnitude < 0.01f) dashDir = new Vector3(1, 0, 0); // 避免零向量
            dashDir.Normalize();

            Vector3 end = GetGroundBelow(p + dashDir * chargeDistance);

            // 3) 前搖提示（朝向 + 短暫停頓）
            if (anim) anim.SetTrigger("PreCharge");
            FaceTowards(start, p);
            // 就位到 start 上方（你原本已有）
            yield return StartCoroutine(MoveHorizontalTo(modelRoot, start + Vector3.up * hoverHeight, 20f));

// 產生前搖圖示（地面箭頭）：從 start 指向 end
            if (chargeTelegraphPrefab != null)
            {
                // 你也可以把寬度/顏色拉到 Inspector，這裡先給好用的預設
                var tele = ChargeTelegraph.Spawn(
                    start, end, chargeTelegraphPrefab,
                    chargeWindup,                 // 播放時間與前搖一致
                    0.35f,                        // 線寬初始
                    0.15f,                        // 線寬結束
                    new Color(1f, 0.35f, 0.2f, 0.95f) // 橘紅
                );

                bool done = false;
                tele.OnFinished += () => done = true;

                // 等圖示跑完（等同等待 wind-up 時間）
                while (!done) yield return null;
            }
            else
            {
                // 沒丟 Prefab 時，用原先的方式等前搖
                yield return new WaitForSeconds(chargeWindup);
            }


            // 4) 衝刺（沿直線從 start -> end），途中用 OverlapCapsule 做線性傷害
            if (anim) anim.SetTrigger("Charge");
            Vector3 prev = modelRoot.position;
            bool damagedOnce = false;

            // 小特效：畫條 Debug 線（方便你在 Scene 看軌跡）
            Debug.DrawLine(start + Vector3.up * 0.1f, end + Vector3.up * 0.1f, Color.cyan, 1.5f);

            // 先落到 start 地面（可選，或維持 hover 高度看你美術演出）
            yield return StartCoroutine(MoveTo(modelRoot, start, descendSpeed));

            // 真正衝刺
            while ((modelRoot.position - end).sqrMagnitude > 0.01f)
            {
                Vector3 next = Vector3.MoveTowards(modelRoot.position, end, chargeSpeed * Time.deltaTime);

                // 膠囊傷害（以上一幀位置 ~ 下一幀位置為段）
                DoCapsuleDamage(prev + Vector3.up * 0.5f, next + Vector3.up * 0.5f, chargeWidth * 0.5f, chargeDamage,
                    ref damagedOnce);

                // 移動 & 面向
                modelRoot.position = next;
                FaceTowards(modelRoot.position, modelRoot.position + dashDir);

                prev = next;
                yield return null;
            }

            // 5) 收招：短暫僵直/冷卻，回到 Idle
            if (anim) anim.SetTrigger("ChargeRecover");
            yield return new WaitForSeconds(chargeRecover);
            _state = BossState.Idle;
        }

// 以 A~B 形成的膠囊做一次傷害（避免多段重複）
        private void DoCapsuleDamage(Vector3 a, Vector3 b, float radius, int damage, ref bool damagedFlag)
        {
            if (damagedFlag) return;
            // 檢查玩家是否在膠囊內
            Collider[] hits = Physics.OverlapCapsule(a, b, radius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].CompareTag("Player"))
                {
                    Vector3 dir = (hits[i].transform.position - transform.position).normalized;
                    var info = new DamageInfo(damage, dir, 2f);
                    if (_playerHP != null) _playerHP.ApplyDamage(info);
                    damagedFlag = true;
                    break;
                }
            }

            // Debug 可視：畫出膠囊外框（簡化為兩個圓）
            DebugDrawCircle(a, radius, 0.1f);
            DebugDrawCircle(b, radius, 0.1f);
        }

        private void FaceTowards(Vector3 from, Vector3 to)
        {
            Vector3 face = to - from;
            face.y = 0f;
            if (face.sqrMagnitude > 0.0001f)
                modelRoot.rotation = Quaternion.Slerp(modelRoot.rotation, Quaternion.LookRotation(face),
                    12f * Time.deltaTime);
        }
    }

    public interface IDamageable
    {
        void TakeDamage(int amount);
    }
}