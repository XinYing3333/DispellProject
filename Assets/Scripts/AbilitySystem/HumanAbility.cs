using UnityEngine;

namespace AbilitySystem
{
    public class HumanAbility : IAbility
    {
        public enum SyncAbilityState
        {
            Idle,
            CloneSpawned,
            Syncing
        }

        private SyncAbilityState currentState = SyncAbilityState.Idle;
        private GameObject cloneInstance;
        private GameObject clonePrefab; // 指定複製體 prefab
        private Transform playerTransform;

        public HumanAbility(GameObject clonePrefab, Transform playerTransform)
        {
            this.clonePrefab = clonePrefab;
            this.playerTransform = playerTransform;
        }

        public void Activate()
        {
            Debug.Log("同步能力已啟用");
            currentState = SyncAbilityState.Idle;
        }

        public void Deactivate()
        {
            Debug.Log("同步能力已停用");
            if (cloneInstance != null)
            {
                GameObject.Destroy(cloneInstance);
                cloneInstance = null;
            }

            currentState = SyncAbilityState.Idle;
        }

        public void Use()
        {
            switch (currentState)
            {
                case SyncAbilityState.Idle:
                    SpawnClone();
                    break;

                case SyncAbilityState.CloneSpawned:
                    StartSync();
                    break;

                case SyncAbilityState.Syncing:
                    DestroyClone();
                    break;
            }
        }

        private void SpawnClone()
        {
            cloneInstance = GameObject.Instantiate(clonePrefab, playerTransform.position, playerTransform.rotation);
            Debug.Log("已生成複製體");
            currentState = SyncAbilityState.CloneSpawned;
        }

        private void StartSync()
        {
            if (cloneInstance != null)
            {
                /*var syncController = cloneInstance.GetComponent<CloneSyncController>();
                if (syncController != null)
                {
                    syncController.StartSyncing(playerTransform);
                    Debug.Log("複製體開始同步動作");
                }*/

                currentState = SyncAbilityState.Syncing;
            }
        }

        private void DestroyClone()
        {
            if (cloneInstance != null)
            {
                GameObject.Destroy(cloneInstance);
                Debug.Log("複製體已消失");
                cloneInstance = null;
            }

            currentState = SyncAbilityState.Idle;
        }
    }
}