using UnityEngine;

[System.Serializable]
public class PhysicsMovementStrategy : IMovementStrategy
{
    public float acceleration = 20f;
    public float deceleration = 10f;
    public float airAcceleration = 30f;
    public float airDeceleration = 15f;

    private bool isTurning;

    public void Move(Entity mover, Rigidbody2D rb, Vector2 direction)
    {
        if (mover is not Player player)
        {
            Debug.LogError("PhysicsMovementStrategy: mover is not a Player");
            return;
        }

        float inputX = direction.x;
        if (inputX == 0)
            return;

        int flip = inputX > 0 ? 1 : -1;
        float scale = Mathf.Abs(player.transform.localScale.x);
        player.transform.localScale = new Vector3(scale * flip, scale, scale);

        float targetVelX = inputX * player.MoveSpeed;
        float currentVelX = rb.linearVelocity.x;

        float accel, decel;
        if (player.IsGrounded)
        {
            accel = acceleration;
            decel = deceleration;
        }
        else
        {
            accel = airAcceleration;
            decel = airDeceleration;
        }

        bool isTurnStarting = (currentVelX > 0.1f && inputX < 0f) || (currentVelX < -0.1f && inputX > 0f);
        float rate = accel;
        float absCntSpeedPer = Mathf.Abs(currentVelX) / player.MoveSpeed;
        if (!isTurning && isTurnStarting && absCntSpeedPer >= 0.9f)
        {
            rate = accel + decel;
            isTurning = true;
        }
        if (isTurning)
        {
            player.Machine.ChangeState<PlayerTurnState>();

            if (absCntSpeedPer <= 0.55f)
            {
                isTurning = false;
            }
        }

        float newVelX = Mathf.MoveTowards(currentVelX, targetVelX, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newVelX, rb.linearVelocity.y);
    }
}
