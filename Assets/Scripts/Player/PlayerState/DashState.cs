using System.Collections;
using UnityEngine;

namespace Player.PlayerState
{
    public class DashState : IPlayerState
    {
        private float startTime;

        public void Enter(PlayerStateMachine context)
        {
            context.Animator.SetBool("Dash", true);
            context.MovementData.CanDash = false;
            context.MovementData.IsDashing = true;
            startTime = Time.time;
            context.StartCoroutine(Dash(context));
        }

        private IEnumerator Dash(PlayerStateMachine context)
        {
            Vector3 dashDir = context.MovementData.RawInputMovement.magnitude > 0.1f
                ? context.MovementData.RawInputMovement.normalized
                : context.Transform.forward;

            while (Time.time < startTime + context.MovementData.DashDuration)
            {
                context.Rigidbody.linearVelocity = dashDir * context.MovementData.DashSpeed;
                yield return null;
            }

            context.Rigidbody.linearVelocity = Vector3.zero;
            context.Animator.SetBool("Dash", false);
            context.MovementData.IsDashing = false;

            context.StartCoroutine(DashCooldown(context));
            context.TransitionToState(new IdleState());
        }

        private IEnumerator DashCooldown(PlayerStateMachine context)
        {
            yield return new WaitForSeconds(context.MovementData.DashCooldown);
            context.MovementData.CanDash = true;
        }

        public void Update(PlayerStateMachine context) {}
        public void FixedUpdate(PlayerStateMachine context) {}
        public void Exit(PlayerStateMachine context) {}
    }
}