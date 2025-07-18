using UnityEngine;

namespace Player.PlayerState
{
    public class DefaultMovement : IMovementStrategy
    {
        public void Move(Rigidbody rb, Vector3 inputDirection, float speed)
        {
            Vector3 movement = inputDirection * speed * Time.deltaTime;
            rb.MovePosition(rb.position + movement);
        }
    }
}