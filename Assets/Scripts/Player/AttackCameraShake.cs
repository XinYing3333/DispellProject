using UnityEngine;
using Cinemachine;

public class AttackCameraShake : MonoBehaviour
{
    [SerializeField] private CinemachineImpulseSource impulse;
    [Header("不同招式的晃動倍率")]
    public float lightHit = 0.8f;
    public float heavyHit = 1.6f;

    // 輕攻擊
    public void ShakeLight()
    {
        if (!impulse) return;
        //impulse.m_AmplitudeGain = lightHit;
        impulse.GenerateImpulse();                 // 也可 GenerateImpulse(Vector3 forceDir)
    }

    // 重攻擊
    public void ShakeHeavy()
    {
        if (!impulse) return;
        //impulse.m_AmplitudeGain = heavyHit;
        impulse.GenerateImpulse();
    }
}