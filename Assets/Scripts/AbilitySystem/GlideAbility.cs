using UnityEngine;

namespace AbilitySystem
{
    public class GlideAbility : IAbility
    {
        private readonly Rigidbody playerRb;
        private readonly GameObject birdPrefab;
        private GameObject birdInstance;
        
        private readonly float minGlideHeight = 1.5f;

        private bool isGliding = false;

        private Animator anim;

        public GlideAbility(GameObject birdPrefab, Rigidbody playerRb)
        {
            this.playerRb = playerRb;
            this.birdPrefab = birdPrefab;
        }

        public void Tick()
        {
            if (isGliding)
            {
                MaintainGlide();
            }
        }

        
        public void Activate()
        {
            Debug.Log("巨鳥之力啟用");

            //============= 亂寫的，要修 ==================
            birdInstance = GameObject.Instantiate(birdPrefab, playerRb.position, Quaternion.identity);
            birdInstance.transform.SetParent(playerRb.transform);
            birdInstance.transform.localPosition = new Vector3(-0.02f, 1.3f, 0.05f);
            birdInstance.SetActive(false);
            anim = playerRb.gameObject.GetComponent<Animator>();
        }

        public void Deactivate()
        {
            Debug.Log("巨鳥之力停用");
            EndGlide();
        }

        public void Use()
        {
            if (!CanGlide()) return;

            if (!isGliding)
            {
                StartGlide();
            }

            MaintainGlide();
        }

        private void StartGlide()
        {
            isGliding = true;
            Debug.Log("開始緩降");
        }

        private void MaintainGlide()
        {
            if (!isGliding) return;

            Vector3 vel = playerRb.linearVelocity;

            // 限制下墜速度：如果掉得太快就放慢
            if (vel.y < -1f)
            {
                vel.y = -1f;
                playerRb.linearVelocity = vel;
                
                //============= 亂寫的，要修 ==================
                birdInstance.SetActive(true);
                anim.SetBool("IsLedgeGrabbing", true);
                //============= 亂寫的，要修 ==================
            }
        }

        private void EndGlide()
        {
            if (!isGliding) return;

            //============= 亂寫的，要修 ==================
            birdInstance.SetActive(false);
            anim.SetBool("IsLedgeGrabbing", false);
            //============= 亂寫的，要修 ==================

            
            isGliding = false;
            Debug.Log("結束緩降");
        }

        private bool CanGlide()
        {
            // 玩家必須在空中，並高於一定距離才可啟用緩降
            return !IsGrounded() && playerRb.linearVelocity.y < 0f && playerRb.transform.position.y > minGlideHeight;
        }

        bool IsGrounded()
        {            
            isGliding = false;
            Vector3 origin = GameObject.FindGameObjectWithTag("Player").transform.position + Vector3.up * 0.1f;
            return Physics.Raycast(origin, Vector3.down, 0.2f, LayerMask.GetMask("Ground"));
        }
    }
}