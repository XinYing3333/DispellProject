using UnityEngine;
using UnityEngine.VFX;

namespace Player.PlayerState
{
    [System.Serializable]
    public class MovementData
    {
        public float MovementSpeed = 2f;
        public float RunSpeed = 4.5f;
        public float TurnSpeed = 10f;
        public float CurrentSpeed = 0f;
        public bool IsDashing = false;

        public VisualEffect StepVFX;
        public SFXType CurrentMoveState;
        public bool IsFootstepPlaying = false;
        public Vector3 RawInputMovement = Vector3.zero;
    }

    public class PlayerStateMachine : MonoBehaviour
    {
        public IMovementStrategy MovementStrategy { get; set; }
        public float MoveSpeed = 4f;
        public Transform CameraTransform;

        private IPlayerState currentState;

        public Transform Transform => transform;
        public Rigidbody Rigidbody { get; private set; }
        public Animator Animator { get; private set; }
        public PlayerInputHandler Input { get; private set; }

        public MovementData MovementData { get; private set; } = new MovementData();

        private void Awake()
        {
            Animator = GetComponent<Animator>();
            Rigidbody = GetComponent<Rigidbody>();
            MovementStrategy = new DefaultMovement();

            // 嘗試自動尋找 StepVFX
            MovementData.StepVFX = GetComponentInChildren<VisualEffect>();
        }

        private void Start()
        {
            Input = PlayerInputHandler.Instance;
            TransitionToState(new IdleState());
        }

        private void Update()
        {
            currentState?.Update(this);
        }

        private void FixedUpdate()
        {
            currentState?.FixedUpdate(this);
        }

        public void TransitionToState(IPlayerState newState)
        {
            currentState?.Exit(this);
            currentState = newState;
            currentState.Enter(this);
        }

        public Vector3 GetCameraRelativeInput()
        {
            Vector2 inputVec = Input.MoveInput;
            Vector3 camForward = CameraTransform.forward;
            Vector3 camRight = CameraTransform.right;
            camForward.y = 0;
            camRight.y = 0;
            return (camForward.normalized * inputVec.y + camRight.normalized * inputVec.x).normalized;
        }
    }
}
