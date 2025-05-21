using UnityEngine;

namespace AbilitySystem
{
    public class HumanAbility : IAbility
    {
        private enum SyncAbilityState
        {
            Clone,
            Moving,
            Remove
        }

        private SyncAbilityState currentState = SyncAbilityState.Clone;

        private GameObject cloneInstance;
        private readonly GameObject clonePrefab;
        private readonly Transform playerTransform;

        public HumanAbility(GameObject clonePrefab, Transform playerTransform)
        {
            this.clonePrefab = clonePrefab;
            this.playerTransform = playerTransform;
        }

        public void Activate()
        {
            Debug.Log("同步能力啟用");
            currentState = SyncAbilityState.Clone;
        }

        public void Deactivate()
        {
            Debug.Log("同步能力停用");
            if (cloneInstance != null)
            {
                GameObject.Destroy(cloneInstance);
                cloneInstance = null;
            }
            currentState = SyncAbilityState.Clone;
        }

        public void Use()
        {
            switch (currentState)
            {
                case SyncAbilityState.Clone:
                    HandleClone();
                    currentState = SyncAbilityState.Moving;
                    break;

                case SyncAbilityState.Moving:
                    HandleStartSync();
                    currentState = SyncAbilityState.Remove;
                    break;

                case SyncAbilityState.Remove:
                    HandleDestroyClone();
                    currentState = SyncAbilityState.Clone;
                    break;
            }
        }

        private void HandleClone()
        {
            if (cloneInstance != null) return;

            cloneInstance = GameObject.Instantiate(clonePrefab, playerTransform.position, playerTransform.rotation);
            Debug.Log("已生成複製體");
        }

        private void HandleStartSync()
        {
            if (cloneInstance == null) return;

            // Clone 開始同步邏輯
            var sync = cloneInstance.GetComponent<CloneMovement>();
            if (sync != null)
            {
                sync.enabled = true;
                Debug.Log("複製體開始同步");
            }
        }

        private void HandleDestroyClone()
        {
            if (cloneInstance == null) return;

            GameObject.Destroy(cloneInstance);
            Debug.Log("複製體已銷毀");
            cloneInstance = null;
        }
    }
}
