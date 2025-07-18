namespace Player.PlayerState
{
    public interface IPlayerState
    {
        void Enter(PlayerStateMachine context);
        void Update(PlayerStateMachine context);
        void FixedUpdate(PlayerStateMachine context);
        void Exit(PlayerStateMachine context);
    }
}