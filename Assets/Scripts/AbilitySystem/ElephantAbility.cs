using UnityEngine;

namespace AbilitySystem
{
    public class ElephantAbility : IAbility
    {
        private readonly GameObject elephantPrefab;
        private readonly Transform playerTransform;
        private GameObject elephantInstance;
        private PlayerMovement playerMovement;
        private bool isRiding = false;

        public ElephantAbility(GameObject elephantPrefab, Transform playerTransform, PlayerMovement playerMovement)
        {
            this.elephantPrefab = elephantPrefab;
            this.playerTransform = playerTransform;
            this.playerMovement = playerMovement;
        }

        public void Tick()
        {
            //
        }

        public void Activate()
        {
            Debug.Log("野象之力啟用");
        }

        public void Deactivate()
        {
            if (isRiding)
            {
                ExitElephant();
            }
        }

        public void Use()
        {
            if (isRiding)
                ExitElephant();
            else
                EnterElephant();
        }

        private void EnterElephant()
        {
            if (elephantInstance == null)
            {
                elephantInstance = elephantPrefab;
                elephantInstance.transform.position = playerTransform.position;
            }

            elephantInstance.SetActive(true);

            playerMovement.ApplyElephantStats();
            isRiding = true;

            Debug.Log("騎乘野象！");
        }


        private void ExitElephant()
        {
            if (elephantInstance != null)
                elephantInstance.SetActive(false);

            playerMovement.RestoreDefaultStats();
            isRiding = false;

            Debug.Log("下象！");
        }

    }
}
