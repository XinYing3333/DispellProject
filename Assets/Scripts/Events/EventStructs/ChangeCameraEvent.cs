using Cinemachine;

namespace Events
{
    public struct ChangeCameraEvent
    {
        public ICinemachineCamera CameraToActivate;

        public ChangeCameraEvent(ICinemachineCamera cam)
        {
            CameraToActivate = cam;
        }
    }

}