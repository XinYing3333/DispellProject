// DeathTransitionOrchestrator.cs
using System.Collections;
using EventBus.Events.Health;
using Unity.VisualScripting;
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
    [Range(0.05f, 1f)] public float slowMoScale = 0.2f;

    [Header("Reload")]
    [SerializeField] private bool reloadSameScene = false;
    public string sceneNameOverride = ""; // 若不為空，將載入該場景

    private EventBinding<OnPlayerDeath> _eventPlayerDeath;
    private EventBinding<OnPlayerRespawn> _eventPlayerRespawn;

    private Health playerHP;
    private RespawnController playerRespawn;

    // ⭐ 新增：作為 RespawnAtLastSafe 的備援出生點（優先取 SpawnOnSceneLoaded.defaultSpawnPoint）
    private Transform _fallbackSpawn;

    private bool _running;

    private void Start()
    {
        // 預設不重載場景，死亡只做就地重生（你可以依需求改回 true）
        reloadSameScene = false;

        var player = GameObject.FindWithTag("Player");
        if (player)
        {
            playerHP      = player.GetComponent<Health>();
            playerRespawn = player.GetComponent<RespawnController>();
        }

        // ⭐ 取得場景預設點作為備援（找不到就先用玩家目前 Transform）
        var loader = FindObjectOfType<SpawnOnSceneLoaded>();
        if (loader && loader.defaultSpawnPoint)
            _fallbackSpawn = loader.defaultSpawnPoint;
        else if (player)
            _fallbackSpawn = player.transform;
    }

    private void OnEnable()
    {
        _eventPlayerDeath   = new EventBinding<OnPlayerDeath>(OnPlayerDead);
        _eventPlayerRespawn = new EventBinding<OnPlayerRespawn>(Play);

        EventBus<OnPlayerDeath>.Register(_eventPlayerDeath);
        EventBus<OnPlayerRespawn>.Register(_eventPlayerRespawn);
    }

    private void OnDisable()
    {
        EventBus<OnPlayerDeath>.Deregister(_eventPlayerDeath);
        EventBus<OnPlayerRespawn>.Deregister(_eventPlayerRespawn);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) // 測試鍵
            Play();
    }

    public void Play()
    {
        if (_running) return;
        StartCoroutine(CoPlay());
    }

    public void OnPlayerDead()
    {
        // 依需求切換：若你想死亡時重載場景，這裡設 true；若只想就地重生，設 false
        reloadSameScene = true;
        StartCoroutine(CoPlay());
    }

    private IEnumerator CoPlay()
    {
        _running = true;

        // 記錄/套用 TimeScale
        float originalScale = Time.timeScale;
        if (slowMoOnDeath) Time.timeScale = slowMoScale;

        // 執行可替換的轉場效果（黑屏）
        if (effect) yield return effect.Play();

        // 保持黑屏片刻（用 realtime 避免受 timescale 影響）
        yield return new WaitForSecondsRealtime(holdBlack);

        // 還原時間
        Time.timeScale = originalScale;

        // ---- A) 重載場景路徑 ----
        if (reloadSameScene)
        {
            string sceneToLoad = string.IsNullOrEmpty(sceneNameOverride)
                ? SceneManager.GetActiveScene().name
                : sceneNameOverride;

            Debug.Log("[DeathTransition] Reloading scene: " + sceneToLoad);
            SceneManager.LoadScene(sceneToLoad);
            _running = false;
            yield break; // 重載後後續不用執行
        }

        // ---- B) 不重載，直接在同場景 RespawnAtLastSafe ----
        if (playerRespawn)
        {
            // ⭐ 關鍵：把備援位置/旋轉傳進去
            Vector3 fbPos = _fallbackSpawn ? _fallbackSpawn.position : playerRespawn.transform.position;
            Quaternion fbRot = _fallbackSpawn ? _fallbackSpawn.rotation : playerRespawn.transform.rotation;

            playerRespawn.RespawnAtLastSafe(fbPos, fbRot);
        }
        else
        {
            Debug.LogWarning("[DeathTransition] playerRespawn not found, cannot respawn.");
        }

        // 回到可視
        if (effectToTransparent) yield return effectToTransparent.Play();

        _running = false;
    }
}
