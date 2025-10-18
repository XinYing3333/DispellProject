// DeathTransitionOrchestrator.cs

using System;
using System.Collections;
using EventBus.Events.Health;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathTransitionOrchestrator : MonoBehaviour
{
    [Header("Plug a concrete effect here")]
    public TransitionEffect effect; // 可替換：之後改成你的扭曲/手機關屏效果
    public TransitionEffect effectToTransparent; // 可替換：之後改成你的扭曲/手機關屏效果

    [Header("Flow Settings")] [Tooltip("切黑之後停留多久，再重載場景")]
    public float holdBlack = 0.3f;

    [Tooltip("死亡瞬間是否做慢動作（TimeScale）")] public bool slowMoOnDeath = true;
    [Range(0.05f, 1f)] public float slowMoScale = 0.2f;

    [Header("Reload")] [SerializeField] private bool reloadSameScene = false;
    public string sceneNameOverride = ""; // 若不為空，將載入該場景

    private EventBinding<OnPlayerDeath> _eventPlayerDeath;
    private EventBinding<OnPlayerRespawn> _eventPlayerRespawn;
    private Health playerHP;
    RespawnController playerRespawn;
    bool _running;

    private void Start()
    {
        reloadSameScene = false;
        GameObject player = GameObject.FindWithTag("Player");
        playerHP = player.GetComponent<Health>();
        playerRespawn = player.GetComponent<RespawnController>();
    }

    private void OnEnable()
    {
        _eventPlayerDeath = new EventBinding<OnPlayerDeath>(OnPlayerDead);
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
        if (Input.GetKeyDown(KeyCode.P)) //Test
        {
            Play();
        }
    }

    public void Play()
    {
        /*if (_running) return;
        _running = true;*/
        StartCoroutine(CoPlay());
    }

    public void OnPlayerDead()
    {
        reloadSameScene = true;
        StartCoroutine(CoPlay());
    }

    IEnumerator CoPlay()
    {
        // 記錄/套用 TimeScale
        float originalScale = Time.timeScale;
        if (slowMoOnDeath) Time.timeScale = slowMoScale;

        // 執行可替換的轉場效果
        if (effect) yield return effect.Play();

        // 保持黑屏片刻
        yield return new WaitForSecondsRealtime(holdBlack);

        // 還原時間
        Time.timeScale = originalScale;
        if (reloadSameScene)
        {
            Debug.Log("Reloading scene " + SceneManager.GetActiveScene().name);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            yield break;
        }
        playerRespawn.RespawnAtLastSafe();
        if (effectToTransparent) yield return effectToTransparent.Play();
    }
}