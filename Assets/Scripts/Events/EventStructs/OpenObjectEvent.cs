using UnityEngine;

namespace Events
{
    public struct OpenObjectEvent
    {
        public GameObject objectToOpen;

        public OpenObjectEvent(GameObject obj)
        {
            objectToOpen = obj;
        }
    }

}