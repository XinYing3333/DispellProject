using UnityEngine;

namespace Player.PlayerState
{
    public interface IMovementStrategy
    {
        void Move(Rigidbody rb, Vector3 inputDirection, float speed);
    }

}