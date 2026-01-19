using System;
using UnityEngine;
using EventBus.Events.Collect;

namespace DefaultNamespace
{
    public class CenterPoint : MonoBehaviour
    {
        [SerializeField] private int requiredAmount;
        [SerializeField] private GameObject interactIcon;
        private bool isTriggered = false;


        private void Start()
        {
           interactIcon.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                if (!isTriggered)
                {
                    EventBus<OnDoorCollectStarted>.Raise(new OnDoorCollectStarted(requiredAmount));
                    isTriggered = true;
                }
                else
                {
                    interactIcon.SetActive(true);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                interactIcon.SetActive(false);
            }
        }
    }
}