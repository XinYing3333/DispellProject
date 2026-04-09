using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RumbleManager : MonoBehaviour
{
    public static RumbleManager Instance { get; private set; }

    private Coroutine _rumbleCoroutine;
    private bool _isPersistentRumbling;
    private float _lowFreq;
    private float _highFreq;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 觸發一次性震動（如：受傷、射擊）
    /// </summary>
    public void Rumble(float lowFreq, float highFreq, float duration)
    {
        Gamepad pad = Gamepad.current;
        if (pad == null) return;

        if (_rumbleCoroutine != null) StopCoroutine(_rumbleCoroutine);
        _rumbleCoroutine = StartCoroutine(RumbleRoutine(lowFreq, highFreq, duration, pad));
    }

    /// <summary>
    /// 開始持續震動（如：蓄力、吸收物體）
    /// </summary>
    public void StartPersistentRumble(float lowFreq, float highFreq)
    {
        _lowFreq = lowFreq;
        _highFreq = highFreq;
        _isPersistentRumbling = true;
        ApplyRumble(lowFreq, highFreq);
    }

    /// <summary>
    /// 停止持續震動
    /// </summary>
    public void StopPersistentRumble()
    {
        _isPersistentRumbling = false;
        ApplyRumble(0, 0);
    }

    private void Update()
    {
        // 確保在持續震動狀態下，即便有其他協程干擾也能恢復強度
        if (_isPersistentRumbling && _rumbleCoroutine == null)
        {
            ApplyRumble(_lowFreq, _highFreq);
        }
    }

    private IEnumerator RumbleRoutine(float low, float high, float duration, Gamepad pad)
    {
        pad.SetMotorSpeeds(low, high);
        yield return new WaitForSecondsRealtime(duration);
        
        // 結束後檢查是否需要恢復到持續震動強度，否則清零
        if (_isPersistentRumbling)
            pad.SetMotorSpeeds(_lowFreq, _highFreq);
        else
            pad.SetMotorSpeeds(0, 0);

        _rumbleCoroutine = null;
    }

    private void ApplyRumble(float low, float high)
    {
        // 如果時間暫停，強制強度為 0
        if (Time.timeScale <= 0)
        {
            Gamepad.current?.SetMotorSpeeds(0, 0);
            return;
        }

        if (Gamepad.current == null) return;
        Gamepad.current.SetMotorSpeeds(low, high);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        ApplyRumble(0, 0);
    }

    private void OnApplicationQuit()
    {
        ApplyRumble(0, 0);
    }
}