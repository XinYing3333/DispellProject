using UnityEngine;
using Cinemachine;
using Events;

public class CameraPrioritySetter : MonoBehaviour
{
    [SerializeField] private int activePriority = 100;
    [SerializeField] private int inactivePriority = 1;

    private CinemachineVirtualCameraBase virtualCam;

    private void Awake()
    {
        virtualCam = GetComponent<CinemachineVirtualCameraBase>();
    }

    private void OnEnable()
    {
        EventBus<ChangeCameraEvent>.Subscribe(OnChangeCamera);
    }

    private void OnDisable()
    {
        EventBus<ChangeCameraEvent>.Unsubscribe(OnChangeCamera);
    }

    private void OnChangeCamera(ChangeCameraEvent e)
    {
        if ((CinemachineVirtualCameraBase)e.CameraToActivate == virtualCam)
            virtualCam.Priority = activePriority;
        else
            virtualCam.Priority = inactivePriority;
    }
}