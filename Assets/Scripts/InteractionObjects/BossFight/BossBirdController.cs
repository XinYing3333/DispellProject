using System.Collections;
using DefaultNamespace.Thought;
using DG.Tweening;
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
            if (!isStun) return;
            if (isStunned) return;

            isStunned = true;
            anim.Play("birld-ani-damage");
    
            // 清理可能殘留的預警物件 (含 Landing 與 Charge)
            if (_landing is LandingAttack landingAtt) landingAtt.Interrupt();
            if (_charge is ChargeAttack chargeAtt) chargeAtt.Interrupt();

            modelRoot.DOKill();
            transform.DOKill();

            StopAllCoroutines(); 
            StartCoroutine(ResumeLoop());
        }

        private IEnumerator ResumeLoop()
        {
            // 1. 等待硬直時間結束
            yield return new WaitForSeconds(2f);
            anim.Play("bird-fly-ani");

            // 2. 獲取當前座標下方的地面高度，並加上預設的盤旋高度
            Vector3 currentPos = modelRoot.position;
            Vector3 groundPos = services.GetGroundBelow(currentPos);
            float targetY = groundPos.y + hoverHeight;

            // 3. 優先執行垂直升空，回到空中預備位置
            yield return services.MoveVerticalTo(modelRoot, targetY, riseSpeed);

            // 4. 釋放硬直鎖定，進入常規攻擊循環
            isStunned = false;
            StartCoroutine(Phase1Loop());
        }

        private void OnBossDead()
        {
            Debug.Log("Boss 死亡！");
            anim.Play("birld-ani-dead");
            
            // 死亡時也必須清理所有預警物件
            if (_landing is LandingAttack landingAtt) landingAtt.Interrupt();
            if (_charge is ChargeAttack chargeAtt) chargeAtt.Interrupt();

            modelRoot.DOKill();
            transform.DOKill();
            
            StopAllCoroutines();
            StartCoroutine(ShowDemoCanvas());
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
