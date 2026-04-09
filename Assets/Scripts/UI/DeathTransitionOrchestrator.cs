using System.Collections;
using EventBus.Events.Health;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathTransitionOrchestrator : MonoBehaviour
{
    [Header("Plug a concrete effect here")]
    public TransitionEffect effect;              // 黑屏（或你要的死亡轉場）
    public TransitionEffect effectToTransparent; // 回到可視的轉場

    [Header("Flow Settings")]
    [Tooltip("切黑之後停留多久，再重載/重生")]
    public float holdBlack = 0.3f;

    [Tooltip("死亡瞬間是否做慢動作（TimeScale）")]
    public bool slowMoOnDeath = true;
    [Range(0.05f, 1f)] public float slowMoScale = 0.1f;

    [Header("Reload")]
    [SerializeField] private bool reloadSameScene = false;
    public string sceneNameOverride = ""; // 若不為空，將載入該場景

    private EventBinding<OnPlayerDeath> _eventPlayerDeath;
    private EventBinding<OnPlayerRespawn> _eventPlayerRespawn;

    private Health playerHP;
    private RespawnController playerRespawn;
    private Transform _fallbackSpawn;

    private bool _running;

    // -------- 生命週期 --------
    private void OnEnable()
    {
        _eventPlayerDeath   = new EventBinding<OnPlayerDeath>(OnPlayerDead);
        _eventPlayerRespawn = new EventBinding<OnPlayerRespawn>(Play);

        EventBus<OnPlayerDeath>.Register(_eventPlayerDeath);
        EventBus<OnPlayerRespawn>.Register(_eventPlayerRespawn);

        SceneManager.sceneLoaded += OnSceneLoaded;
        // 嘗試先抓一次（若此時拿不到也沒關係，之後會重試）
        EnsurePlayerRefs();
    }

    private void OnDisable()
    {
        EventBus<OnPlayerDeath>.Deregister(_eventPlayerDeath);
        EventBus<OnPlayerRespawn>.Deregister(_eventPlayerRespawn);
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // 場景變了，清快取，下一次用到時再抓
        playerHP = null;
        playerRespawn = null;
        _fallbackSpawn = null;

        // 等下一幀再抓，避免載入中的時序
        StartCoroutine(CoDelayEnsureRefs());
    }

    private IEnumerator CoDelayEnsureRefs()
    {
        yield return null;
        EnsurePlayerRefs();
    }

    // -------- 公開 API / 事件回呼 --------
    public void Play()
    {
        if (_running) return;
        // 強制把重載關掉，避免先前死亡把旗標留著
        reloadSameScene = false;
        StartCoroutine(CoPlay());
    }


    public void OnPlayerDead()
    {
        // 依需求：死亡時是否重載場景
        reloadSameScene = true;
        if (_running) return;
        StartCoroutine(CoPlay());
    }

    // -------- 主流程 --------
    private IEnumerator CoPlay()
    {
        _running = true;

        // 惰性確保引用（Build/地址化/加載時序下很重要）
        yield return EnsurePlayerRefsYield();

        float originalScale = Time.timeScale;
        if (slowMoOnDeath) Time.timeScale = slowMoScale;

        if (effect) yield return effect.Play();
        yield return new WaitForSecondsRealtime(holdBlack);
        Time.timeScale = originalScale;

        if (reloadSameScene)
        {
            string sceneToLoad = string.IsNullOrEmpty(sceneNameOverride)
                ? SceneManager.GetActiveScene().name
                : sceneNameOverride;

            Debug.Log("[DeathTransition] Reloading scene: " + sceneToLoad);
            SceneManager.LoadScene(sceneToLoad);
            _running = false;
            yield break;
        }

        playerRespawn = GameObject.FindGameObjectWithTag("Player").GetComponent<RespawnController>();
        
        // 不重載，直接在同場景重生
        if (playerRespawn)
        {
            Vector3 fbPos = _fallbackSpawn ? _fallbackSpawn.position : playerRespawn.transform.position;
            Quaternion fbRot = _fallbackSpawn ? _fallbackSpawn.rotation : playerRespawn.transform.rotation;
            playerRespawn.RespawnAtLastSafe(fbPos, fbRot);
        }
        else
        {
            Debug.LogWarning("[DeathTransition] playerRespawn not found, cannot respawn.");
        }

        if (effectToTransparent) yield return effectToTransparent.Play();

        _running = false;
    }

    // -------- 參考抓取（可重複呼叫，保證安全） --------
    private void EnsurePlayerRefs()
    {
        if (playerRespawn && playerHP && _fallbackSpawn) return;

        // 先找 Player（允許非啟用與不同層次）
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (!playerGO)
        {
            // 有些專案玩家一開始是關閉的或沒打 Tag → 退而求其次
            var resp = FindObjectOfType<RespawnController>(true);
            if (resp) playerGO = resp.gameObject;
        }

        if (playerGO)
        {
            playerHP      = playerGO.GetComponent<Health>()       ?? playerHP;
            playerRespawn = playerGO.GetComponent<RespawnController>() ?? playerRespawn;
        }

        // 備援出生點
        if (_fallbackSpawn == null)
        {
            var loader = FindObjectOfType<SpawnOnSceneLoaded>(true);
            if (loader && loader.defaultSpawnPoint) _fallbackSpawn = loader.defaultSpawnPoint;
            else if (playerGO) _fallbackSpawn = playerGO.transform;
        }
    }

    // 當下拿不到時，等到拿到為止（最多等數幀避免無限等）
    private IEnumerator EnsurePlayerRefsYield(int maxFrames = 30)
    {
        int frames = 0;
        while ((!playerRespawn || !playerHP || !_fallbackSpawn) && frames < maxFrames)
        {
            EnsurePlayerRefs();
            if (playerRespawn && playerHP && _fallbackSpawn) break;
            frames++;
            yield return null;
        }
    }
}
