using UnityEngine;

[System.Serializable]
public class ConstantVelocityMovementStrategy : IMovementStrategy
{
    public void Move(Entity mover, Rigidbody2D rb, Vector2 direction)
    {
        rb.linearVelocity = new Vector2(direction.x * mover.MoveSpeed, rb.linearVelocity.y);
    }
}
