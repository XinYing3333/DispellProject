using System.Collections;
using DefaultNamespace.Thought;
using Player;
using Player.InteractionSystem;
using SpellSystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace BossFight
{
    public class BossBirdController : MonoBehaviour ,IHitReceiver,ISpellAffectable
    {
        public enum BossState { Idle, Attacking, Stunned }

        [SerializeField] private DemoShow demoCanvas;
        
        [Header("Refs")]
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Animator anim;
        [SerializeField] private Transform player;
        [SerializeField] private BossServices services;
        [FormerlySerializedAs("health")] [SerializeField] private BossHealth bossHealth;
        [SerializeField] private LandingTelegraph telegraphPrefab;
        [SerializeField] private ChargeTelegraph  chargeTelegraphPrefab;
        [SerializeField] private ThoughtPayloadSO requiredWeakness; // 在 Inspector 指定這隻 Boss 怕哪種念頭（石頭帶有的那種）
                
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

            bossHealth.OnDamaged += OnBossDamaged;
            bossHealth.OnDead += OnBossDead;
            
            StartCoroutine(Phase1Loop());
        }

        private IEnumerator Phase1Loop()
        {
            int landingCount = 0;
            while (true)
            {
                _state = BossState.Attacking;
                // 每次攻擊 Execute 內部都包含了：升空 -> 攻擊 -> 落地 -> 硬直 -> 回到空中
                if (landingCount < landingsPerCharge)
                {
                    yield return _landing.Execute(_ctx);
                    landingCount++;
                }
                else
                {
                    yield return _charge.Execute(_ctx);
                    landingCount = 0;
                }

                _state = BossState.Idle;
                // Idle 期間如果需要微幅上下漂浮，可在這裡加入一個 Float 協程
                yield return new WaitForSeconds(landingIdleBetween);
            }
        }
        
        public void HandleHitTrigger(Collider other)
        {
            if (other.TryGetComponent(out Spell spell))
            {
                //bossHealth.TakeDamage(1);
            }
        }
        
        private void OnBossDamaged(bool isStun)
        {
            Debug.Log($"Boss 受到攻擊！(觸發硬直: {isStun})");

            // 狀態：輕擊（如法術）。僅扣除血量，不影響當前攻擊協程與動畫運作。
            if (!isStun) return;

            // 狀態：重擊（如石頭）。避免連續硬直覆寫。
            if (isStunned) return;

            // 執行硬直中斷邏輯
            isStunned = true;
            anim.Play("birld-ani-damage");
    
            // 強制中止 Phase1Loop 與所有關聯攻擊行為
            StopAllCoroutines(); 
            StartCoroutine(ResumeLoop());
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

            StartCoroutine(ShowDemoCanvas());
            // 可觸發勝利 UI、掉落、過場動畫
        }
        
        // --------- Demo -----------
        IEnumerator ShowDemoCanvas()
        {
            yield return new WaitForSeconds(5f);
            PlayerInputHandler.Instance.SetLockMovement(true);
            demoCanvas.ShowDemoEndPanel();
            Time.timeScale = 0f;
        }

        public void OnHit(ThoughtPayloadSO payload)
        {
            if (payload == null) return;
            if (payload == requiredWeakness)
            {
                bossHealth.TakeDamage(10);
            }
            else
            {
                Debug.Log("念頭屬性不符，無效攻擊");
            }
        }

        public void OnSpellHit(SpellType spellType, Vector3 hitPoint)
        {
            bossHealth.TakeDamage(1,false);
        }

        public void OnSpellRecall()
        {
            
        }
    }
}
