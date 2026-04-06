using UnityEngine;
using Cinemachine;

public class BossCameraTrigger : MonoBehaviour
{
    [Header("Camera References")]
    public CinemachineFreeLook playerCam;
    public CinemachineFreeLook bossCam;

    [Header("Settings")]
    public int activePriority = 20;
    public int inactivePriority = 5;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 提高 Boss 攝影機優先級，Cinemachine 會自動開始混合 (Blend)
            bossCam.Priority = activePriority;
            
            // 視需求降低原攝影機優先級（非必要，只要 BossCam 較高即可）
            playerCam.Priority = inactivePriority;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 恢復原狀
            bossCam.Priority = inactivePriority;
            playerCam.Priority = activePriority;
        }
    }
}