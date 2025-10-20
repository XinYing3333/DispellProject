using UnityEngine;

namespace AbilitySystem
{
    public class GlideAbility : IAbility
    {
        private readonly Rigidbody playerRb;
        private readonly GameObject birdPrefab;
        private GameObject birdInstance;

        private float glideLiftForce = 20f; // 起飛力
        private float maxFallSpeed = -2f;   // 緩降下墜最大速度
        private float groundCheckDistance = 0.3f;
        
        private float glideStartTime;
        private float glideDelay = 0.2f;

        private bool canGlide = true;
        private bool isGliding = false;
        private Animator anim;

        private Transform playerTransform;

        public GlideAbility(GameObject birdPrefab, Rigidbody playerRb)
        {
            this.playerRb = playerRb;
            this.birdPrefab = birdPrefab;
            this.playerTransform = playerRb.transform;
        }

        public void Tick()
        {
            if (!isGliding && !canGlide && IsGrounded())
            {
                canGlide = true; // 落地後重置
            }

            if (!isGliding) return;
            
            // 加入延遲：起飛剛開始時不判定落地
            if (Time.time - glideStartTime > glideDelay && IsGrounded())
            {
                EndGlide();
                return;
            }

            if (playerRb.linearVelocity.y < 0f)
            {
                MaintainGlide();
            }
        }



        public void Activate()
        {
            Debug.Log("巨鳥之力啟用");

            if (birdInstance == null)
            {
                birdInstance = birdPrefab;
                birdInstance.transform.SetParent(playerTransform);
                birdInstance.transform.localPosition = new Vector3(-0.02f, 1.3f, 0.05f);
            }

            birdInstance.SetActive(false);

            anim = playerTransform.GetComponent<Animator>();
        }


        public void Deactivate()
        {
            Debug.Log("巨鳥之力停用");
            EndGlide();
        }

        public void Use()
        {
            if (!isGliding && canGlide)
            {
                StartGlide();
            }
            else if (isGliding)
            {
                EndGlide();
            }
        }


        private void StartGlide()
        {
            isGliding = true;
            glideStartTime = Time.time;

            playerRb.linearVelocity = new Vector3(playerRb.linearVelocity.x, glideLiftForce, playerRb.linearVelocity.z);

            birdInstance?.SetActive(true);
            anim?.SetBool("IsLedgeGrabbing", true);
        }


        private void MaintainGlide()
        {
            Vector3 vel = playerRb.linearVelocity;

            // 緩降限制速度
            if (vel.y < maxFallSpeed)
            {
                vel.y = maxFallSpeed;
                playerRb.linearVelocity = vel;
            }
        }

        private void EndGlide()
        {
            if (!isGliding) return;

            birdInstance?.SetActive(false);
            anim?.SetBool("IsLedgeGrabbing", false);

            isGliding = false;
            canGlide = false; // 停止後不能立刻再飛

            Debug.Log("結束緩降");
        }


        private bool IsGrounded()
        {
            Vector3 origin = playerTransform.position + Vector3.up * 0.1f;
            return Physics.Raycast(origin, Vector3.down, groundCheckDistance, LayerMask.GetMask("Ground"));
        }
    }
}
