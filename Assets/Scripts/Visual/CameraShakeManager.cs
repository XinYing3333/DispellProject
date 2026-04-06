using UnityEngine;
using Cinemachine;
using DG.Tweening;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance { get; private set; }

    private CinemachineImpulseSource globalImpulseSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            globalImpulseSource = GetComponent<CinemachineImpulseSource>();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 觸發相機震動
    /// </summary>
    /// <param name="force">震動強度倍率 (預設 1)</param>
    /// <param name="delay">延遲時間</param>
    public void Shake(float force = 1f, float delay = 0f)
    {
        if (globalImpulseSource == null) return;

        if (delay <= 0)
        {
            globalImpulseSource.GenerateImpulse(Vector3.one * force);
        }
        else
        {
            DOVirtual.DelayedCall(delay, () => 
            {
                globalImpulseSource.GenerateImpulse(Vector3.one * force);
            });
        }
    }
}