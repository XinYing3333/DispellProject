using UnityEngine;

namespace Player.PlayerState
{
    public class MoveState : IPlayerState
    {
        public void Enter(PlayerStateMachine context)
        {
            context.Animator.SetFloat("Speed", 0.5f); // 起始速度值
        }

        public void Update(PlayerStateMachine context)
        {
            Vector3 inputDirection = context.GetCameraRelativeInput();
            float speedFactor = Mathf.Lerp(context.MovementData.MovementSpeed, context.MovementData.RunSpeed, context.Input.MoveSpeedMultiplier);
            context.MovementData.CurrentSpeed = Mathf.Lerp(context.MovementData.CurrentSpeed, speedFactor, Time.deltaTime * 10f);

            // 更新動畫參數
            context.Animator.SetFloat("Speed", inputDirection.magnitude > 0.1f ? inputDirection.magnitude : 0f);
            context.MovementData.RawInputMovement = inputDirection;

            if (inputDirection.magnitude < 0.1f)
            {
                context.TransitionToState(new IdleState());
            }
            else if (context.Input.JumpPressed && context.MovementData.JumpCount < context.MovementData.MaxJumpCount)
            {
                context.Input.ResetJump();
                context.TransitionToState(new JumpState());
            }
            else if (context.Input.DashPressed && context.MovementData.CanDash)
            {
                context.Input.ResetDash();
                context.TransitionToState(new DashState());
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