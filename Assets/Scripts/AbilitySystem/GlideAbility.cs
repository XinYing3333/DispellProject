using UnityEngine;

namespace AbilitySystem
{
    public class GlideAbility : IAbility
    {
        private readonly Rigidbody playerRb;
        private readonly float glideGravityScale = 0.5f;
        private readonly float normalGravityScale = 1.0f;
        private readonly float minGlideHeight = 1.5f;

        private bool isGliding = false;

        public GlideAbility(Rigidbody playerRb)
        {
            this.playerRb = playerRb;
        }

        public void Activate()
        {
            Debug.Log("巨鳥之力啟用");
        }

        public void Deactivate()
        {
            Debug.Log("巨鳥之力停用");
            EndGlide();
        }

        public void Use()
        {
            
            StartGlide();
                
        }

        private void StartGlide()
        {
            isGliding = true;
            Physics.gravity = Vector3.up * -9.81f * glideGravityScale;
            Debug.Log("開始緩降");
        }

        private void EndGlide()
        {
            if (!isGliding) return;

            isGliding = false;
            Physics.gravity = Vector3.up * -9.81f * normalGravityScale;
            Debug.Log("結束緩降");
        }

        private bool CanGlide()
        {
            // 玩家必須在空中，並高於一定距離才可啟用緩降
            return !IsGrounded() && playerRb.linearVelocity.y < 0f && playerRb.transform.position.y > minGlideHeight;
        }

        bool IsGrounded()
        {
            Vector3 origin = GameObject.FindGameObjectWithTag("Player").transform.position + Vector3.up * 0.1f;
            return Physics.Raycast(origin, Vector3.down, 0.2f, LayerMask.GetMask("Ground"));
        }
    }
}