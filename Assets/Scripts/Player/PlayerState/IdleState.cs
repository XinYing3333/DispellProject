namespace Player.PlayerState
{
    public class IdleState : IPlayerState
    {
        public void Enter(PlayerStateMachine context)
        {
            context.Animator.SetFloat("Speed", 0f);
        }

        public void Update(PlayerStateMachine context)
        {
            var input = context.Input.MoveInput;
            if (input.magnitude > 0.1f)
            {
                context.TransitionToState(new MoveState(context));
            }
        }

        public void FixedUpdate(PlayerStateMachine context) { }
        public void Exit(PlayerStateMachine context) { }
    }
}