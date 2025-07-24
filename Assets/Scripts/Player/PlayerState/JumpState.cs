using UnityEngine;

namespace Player.PlayerState
{
    public class JumpState : IPlayerState
    {
        public void Enter(PlayerStateMachine context)
        {
            context.MovementData.JumpCount++;
            context.Animator.SetFloat("Speed", 0f); // 停止跑步動畫

            if (context.MovementData.JumpCount == 1)
            {
                context.Animator.SetBool("Jump", true);
                context.Animator.SetBool("IsDoubleJump", false);
            }
            else
            {
                context.Animator.SetBool("IsDoubleJump", true);
                context.Animator.SetBool("Jump", false);
            }

            context.Rigidbody.linearVelocity = new Vector3(
                context.Rigidbody.linearVelocity.x,
                context.MovementData.JumpForce,
                context.Rigidbody.linearVelocity.z
            );
        }

        public void Update(PlayerStateMachine context)
        {
            if (context.IsGrounded())
            {
                context.MovementData.JumpCount = 0;
                context.Animator.SetBool("Jump", false);
                context.Animator.SetBool("IsDoubleJump", false);
                context.TransitionToState(new IdleState());
            }
        }

        public void FixedUpdate(PlayerStateMachine context)
        {
            Vector3 move = context.MovementData.RawInputMovement * (context.MovementData.CurrentSpeed * Time.fixedDeltaTime);
            context.Rigidbody.MovePosition(context.Rigidbody.position + move);

            if (context.MovementData.RawInputMovement.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(context.MovementData.RawInputMovement);
                context.Rigidbody.rotation = Quaternion.Slerp(context.Rigidbody.rotation, targetRot, context.MovementData.TurnSpeed * Time.fixedDeltaTime);
            }
        }
        public void Exit(PlayerStateMachine context) {}
    }
}
