using UnityEngine;

public interface IMovementStrategy
{
    void Move(Entity mover, Rigidbody2D rb, Vector2 direction);
    void UpdateFacing(Entity mover, float inputX);
}
