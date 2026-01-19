using System.Collections;
using UnityEngine;

/// <summary>
/// 兩階段法術彈：飛行(像火球) → 引爆(濃煙雲)
/// - 支援：計時引爆 / 碰撞引爆 / 微導引
/// - 引爆後：隱藏彈體、關閉碰撞、生成Smoke FX(存活一段時間)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class Spell : MonoBehaviour
{
    [Header("Visual / FX")]
    [Tooltip("飛行(火球)時播放的粒子/尾焰(可選)")]
    public GameObject travelFxPrefab;
    [Tooltip("引爆後生成的濃煙雲Prefab(需要自帶粒子/體積遮擋等)")]
    public GameObject smokeFxPrefab;

    [Tooltip("引爆時播放的SFX(可選)")]
    public SFXType explodeSfx = SFXType.Spawn;

    [Header("Flight")]
    [Tooltip("是否啟用微導引(空目標則不導引)")]
    public bool enableHoming = false;
    public float homingSpeed = 8f;      // 期望速度
    public float rotateSpeed = 8f;      // 導引插值(越大越快貼近目標)
    [Tooltip("飛行最大壽命(秒)；到時未碰撞也會引爆成煙霧")]
    public float fuseLifetime = 0.35f;

    [Header("Impact")]
    [Tooltip("是否在碰撞時立即引爆")]
    public bool explodeOnCollision = true;
    [Tooltip("允許觸發碰撞引爆的圖層")]
    public LayerMask collideMask = ~0;  // 預設全部

    [Header("Smoke")]
    [Tooltip("煙霧持續時間(秒)，到時自動銷毀FX與本彈體")]
    public float smokeDuration = 2.0f;
    [Tooltip("引爆後延遲多少秒再Destroy(給FX收尾)")]
    public float cleanupDelay = 0.08f;

    [Header("Optional")]
    public SpellType spellType = SpellType.AttackSpell; // 仍保留你的型別

    // --- 內部狀態 ---
    private Rigidbody _rb;
    private Collider  _col;
    private MeshRenderer _mesh;     // 你的舊版用到，保留以便快速隱藏
    private GameObject _travelFxInst;

    private float _life;
    private bool _exploded;
    private Transform _target;      // 導引目標(可外部指定)

    // ====== 外部API：設定導引目標(你的ThrowingSystem或AimAssist可呼叫) ======
    public void SetTarget(Transform t) => _target = t;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        _mesh = GetComponent<MeshRenderer>(); // 可能為空，沒關係
        _life = fuseLifetime;
    }

    void Start()
    {
        // 飛行尾焰
        // if (travelFxPrefab)
        //     _travelFxInst = Instantiate(travelFxPrefab, transform);

        // （可選）依SpellType給個快速著色或特效開關
        if (_mesh)
        {
            switch (spellType)
            {
                case SpellType.AttackSpell:     _mesh.material.color = new Color(1f, 0.8f, 0.2f); break;
                case SpellType.RotateSpell:     _mesh.material.color = new Color(0.4f, 1f, 0.5f); break;
                case SpellType.ElectricBullet:  _mesh.material.color = new Color(1f, 0.2f, 0.2f); break;
            }
        }
    }

    void Update()
    {
        if (_exploded) return;

        // 保險絲時間到了 → 引爆
        _life -= Time.deltaTime;
        if (_life <= 0f)
        {
            ExplodeAsSmoke();
            return;
        }
    }

    void FixedUpdate()
    {
        if (_exploded) return;
        if (!enableHoming || _target == null) return;

        // 微導引：把速度朝向目標方向插值
        Vector3 dir = (_target.position - transform.position);
        if (dir.sqrMagnitude < 1e-6f) return;

        dir.Normalize();
        Vector3 desiredVel = dir * homingSpeed;
        _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, desiredVel, rotateSpeed * Time.fixedDeltaTime);

        // 讓朝向貼合速度(若需要)
        if (_rb.linearVelocity.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(_rb.linearVelocity.normalized, Vector3.up);
    }

    // —— 碰撞 → 視圖層/設定決定是否引爆
    void OnCollisionEnter(Collision other)
    {
        if (_exploded || !explodeOnCollision) return;
        if (((1 << other.gameObject.layer) & collideMask) == 0) return;
        ExplodeAsSmoke();
    }

    void OnTriggerEnter(Collider other)
    {
        // 如果你的彈體用Trigger也想引爆，放開這段
        if (_exploded || !explodeOnCollision) return;
        if (((1 << other.gameObject.layer) & collideMask) == 0) return;
        ExplodeAsSmoke();
    }

    // ====== 核心：轉換為濃煙態 ======
    private void ExplodeAsSmoke()
    {
        if (_exploded) return;
        _exploded = true;

        // 音效
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(explodeSfx);

        // 關閉可見與碰撞，停止運動
        if (_mesh) _mesh.enabled = false;
        if (_col)  _col.enabled = false;
        if (_travelFxInst) Destroy(_travelFxInst);
        _rb.linearVelocity = Vector3.zero;
        _rb.isKinematic = true;
        _rb.useGravity = false;

        // 生成濃煙FX（例如帶有體積雲、湍流Shader、屏障Collider等）
        GameObject fx = null;
        if (smokeFxPrefab)
        {
            fx = Instantiate(smokeFxPrefab, transform.position, Quaternion.identity);
            // 可選：讓FX跟著地面法線對齊
            // fx.transform.up = Vector3.up;
        }

        // 啟動壽命流程：smokeDuration 結束 → 清理
        StartCoroutine(Co_SmokeLife(fx));
    }

    private IEnumerator Co_SmokeLife(GameObject fx)
    {
        // 在這段時間內，FX可以自己做擴散/濃度變化/遮蔽視線等
        yield return new WaitForSeconds(smokeDuration);

        // 讓粒子收尾一下
        if (fx) Destroy(fx, cleanupDelay);

        // 再刪掉本體
        Destroy(gameObject, cleanupDelay);
    }
}
