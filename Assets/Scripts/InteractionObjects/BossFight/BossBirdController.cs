using System.Collections;
using UnityEngine;

namespace BossFight
{
    public class BossBirdController : MonoBehaviour
    {
        public enum BossState { Idle, Attacking, Stunned }

        [Header("Refs")]
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Animator anim;
        [SerializeField] private Transform player;
        [SerializeField] private BossServices services;
        [SerializeField] private BossHealth health;
        [SerializeField] private LandingTelegraph telegraphPrefab;
        [SerializeField] private ChargeTelegraph  chargeTelegraphPrefab;
                
        [Header("Landing Settings")]  public float hoverHeight = 35f;
        public float riseSpeed = 20f;
        public float descendSpeed = 22f;
        public float minLandDistFromPlayer = 4f;
        public float maxLandDistFromPlayer = 9f;
        public float groundOffset = 1.5f;

        [Header("Pattern")]
        [SerializeField] private int landingsPerCharge = 2;
        [SerializeField] private float landingIdleBetween = 0f;
        [SerializeField] private float chargeIdleBetween  = 0f;

        [Header("Configs")]
        [SerializeField] private LandingConfig landingCfg = new();
        [SerializeField] private ChargeConfig  chargeCfg  = new();

        // 供攻擊讀取（也可搬到 Services/Config）
        public float HoverHeight  => hoverHeight;
        public float RiseSpeed    => riseSpeed;
        public float DescendSpeed => descendSpeed;
        public float MinLandDist  => minLandDistFromPlayer;
        public float MaxLandDist  => maxLandDistFromPlayer;
        
        private BossState _state = BossState.Idle;
        private Health _playerHP;
        private BossContext _ctx;
        private IBossAttack _landing, _charge;
        private bool isStunned = false;

        private void Start()
        {
            _playerHP = GameObject.FindGameObjectWithTag("Player").GetComponent<Health>();
            if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;

            // 製作 Context
            _ctx = new BossContext { ModelRoot = modelRoot, Player = player, Anim = anim, Services = services, Owner = this };

            // 組裝兩個攻擊
            _landing = new LandingAttack(landingCfg, telegraphPrefab, _playerHP);
            _charge  = new ChargeAttack (chargeCfg,  chargeTelegraphPrefab, _playerHP);

            health.OnDamaged += OnBossDamaged;
            health.OnDead += OnBossDead;
            
            StartCoroutine(Phase1Loop());
        }

        private IEnumerator Phase1Loop()
        {
            int landingCount = 0;
            while (true)
            {
                _state = BossState.Attacking;

                if (landingCount < landingsPerCharge)
                {
                    yield return _landing.Execute(_ctx);
                    landingCount++;
                    _state = BossState.Idle;
                    yield return new WaitForSeconds(landingIdleBetween);
                }
                else
                {
                    yield return _charge.Execute(_ctx);
                    landingCount = 0;
                    _state = BossState.Idle;
                    yield return new WaitForSeconds(chargeIdleBetween);
                }

                // TODO: 這裡檢查血量/條件切到 Phase2：換一組攻擊與 Pattern 即可
            }
        }
        
        public void HandleHitTrigger(Collider other)
        {
            if (other.TryGetComponent(out Spell spell))
            {
                health.TakeDamage(1);
            }
        }
        
        private void OnBossDamaged()
        {
            Debug.Log("Boss 受到攻擊！");
            if(isStunned)return;
            anim.Play("birld-ani-damage");
            StopAllCoroutines(); // 可選：停止攻擊行為
            StartCoroutine(ResumeLoop());
            isStunned = true;
        }

        private IEnumerator ResumeLoop()
        {
            yield return new WaitForSeconds(2f);
            anim.Play("bird-fly-ani");
            StartCoroutine(Phase1Loop());
            isStunned = false;
        }

        private void OnBossDead()
        {
            Debug.Log("Boss 死亡！");
            anim.Play("birld-ani-dead");
            StopAllCoroutines();
            // 可觸發勝利 UI、掉落、過場動畫
        }
    }
}
