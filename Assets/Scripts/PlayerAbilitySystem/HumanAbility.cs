using UnityEngine;
using UnityEngine.AI;

namespace AbilitySystem
{
    public class HumanAbility : IAbility
    {
        private GameObject clonePrefab;
        private GameObject cloneInstance;
        private Transform playerTransform;
        private float followDistance = 1f;
        private float maxDistance = 15f;

        public HumanAbility(GameObject prefab, Transform player)
        {
            clonePrefab = prefab;
            playerTransform = player;
        }

        public void Activate() { }

        public void Deactivate()
        {
            if (cloneInstance != null)
                GameObject.Destroy(cloneInstance);
        }

        private float summonDuration = 8f;
        private float summonTimer = 0f;

        public void Tick()
        {
            /*if (cloneInstance != null)
            {
                summonTimer += Time.deltaTime;

                if (summonTimer >= summonDuration)
                {
                    GameObject.Destroy(cloneInstance);
                    cloneInstance = null;
                    return;
                }

                // 移除高度補位與 NavMesh 控制，改由 CloneFollower 處理跟隨
            }*/
        }

        public void Use()
        {
            if (cloneInstance == null)
            {
                Vector3 spawnPos = playerTransform.position + playerTransform.forward * -1f;
                cloneInstance = GameObject.Instantiate(clonePrefab, spawnPos, Quaternion.identity);

                var follower = cloneInstance.GetComponent<CloneFollower>();
                if (follower != null)
                    follower.player = playerTransform;

                summonTimer = 0f; // reset timer
            }
            else
            {
                GameObject.Destroy(cloneInstance);
                cloneInstance = null;
            }
        }


    }
}

/*
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
        private ParticleSystem particle;

        public HumanAbility(GameObject clonePrefab, Transform playerTransform)
        {
            this.clonePrefab = clonePrefab;
            this.playerTransform = playerTransform;
            this.particle = clonePrefab.GetComponentInChildren<ParticleSystem>();
        }

        public void Tick()
        {
           //
        }
        
        public void Activate()
        {
            currentState = SyncAbilityState.Clone;
        }

        public void Deactivate()
        {
            if (cloneInstance != null)
            {
                cloneInstance.SetActive(false);
                cloneInstance = null;
            }
            currentState = SyncAbilityState.Clone;
        }

        public void Use()
        {
            particle.Play();
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
                    HandleRemoveClone();
                    currentState = SyncAbilityState.Clone;
                    break;
            }
        }

        private void HandleClone()
        {
            if (cloneInstance != null) return;

            Vector3 spawnPosition = playerTransform.position + playerTransform.forward * 1f;
            cloneInstance = clonePrefab;
            cloneInstance.transform.position = spawnPosition;
            cloneInstance.SetActive(true);
            cloneInstance.transform.rotation = playerTransform.rotation;

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

        private void HandleRemoveClone()
        {
            if (cloneInstance == null) return;
            var sync = cloneInstance.GetComponent<CloneMovement>();
            if (sync != null)
            {
                sync.enabled = false;
            }
            cloneInstance.SetActive(false);
            Debug.Log("複製體已銷毀");
            cloneInstance = null;
        }
    }
}
*/
