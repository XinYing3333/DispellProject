using UnityEngine;

namespace Player.PlayerState
{
    public class MoveState : IPlayerState
    {
        private readonly PlayerStateMachine _context;
        private readonly Rigidbody _rb;
        private readonly Animator _anim;
        private readonly Transform _transform;
        private readonly PlayerInputHandler _input;

        public MoveState(PlayerStateMachine context)
        {
            _context = context;
            _rb = context.Rigidbody;
            _anim = context.Animator;
            _transform = context.Transform;
            _input = context.Input;
        }

        public void Enter(PlayerStateMachine context) { }
        public void Update(PlayerStateMachine context)
        {
            if (_context.MovementData.IsDashing) return;

            Vector2 inputMovement = _input.MoveInput;
            Vector3 cameraForward = _context.CameraTransform.forward;
            Vector3 cameraRight = _context.CameraTransform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            Vector3 moveDirection = (cameraForward.normalized * inputMovement.y + cameraRight.normalized * inputMovement.x).normalized;

            float targetSpeed = Mathf.Lerp(_context.MovementData.MovementSpeed, _context.MovementData.RunSpeed, _input.MoveSpeedMultiplier);
            float currentSpeed = Mathf.Lerp(_context.MovementData.CurrentSpeed, targetSpeed, Time.deltaTime * 10f);

            _context.MovementData.CurrentSpeed = currentSpeed;

            _anim.SetFloat("Speed", moveDirection.magnitude * (targetSpeed / _context.MovementData.RunSpeed));

            _rb.MovePosition(_rb.position + moveDirection * (currentSpeed * Time.deltaTime));

            if (moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRotation, _context.MovementData.TurnSpeed * Time.deltaTime);
            }
        }
        public void FixedUpdate(PlayerStateMachine context) { }
        public void Exit(PlayerStateMachine context) { }
    }

}