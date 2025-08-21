// DeathTransitionOrchestrator.cs

using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathTransitionOrchestrator : MonoBehaviour
{
    [Header("Plug a concrete effect here")]
    public TransitionEffect effect;     // 可替換：之後改成你的扭曲/手機關屏效果

    [Header("Flow Settings")]
    [Tooltip("切黑之後停留多久，再重載場景")]
    public float holdBlack = 0.3f;
    [Tooltip("死亡瞬間是否做慢動作（TimeScale）")]
    public bool slowMoOnDeath = true;
    [Range(0.05f, 1f)] public float slowMoScale = 0.2f;

    [Header("Reload")]
    public bool reloadSameScene = true;
    public string sceneNameOverride = ""; // 若不為空，將載入該場景

    bool _running;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Play();
        }
    }

    public void Play()
    {
        if (_running) return;
        _running = true;
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

        // 重載場景
        if (reloadSameScene || string.IsNullOrEmpty(sceneNameOverride))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            SceneManager.LoadScene(sceneNameOverride);
        }
    }
}