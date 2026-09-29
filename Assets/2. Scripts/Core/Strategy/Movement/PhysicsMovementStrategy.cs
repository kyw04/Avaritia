using UnityEngine;

[System.Serializable]
public class PhysicsMovementStrategy : IMovementStrategy
{
    private float acceleration = 20f;
    private float deceleration = 10f;
    private float airAcceleration = 30f;
    private float airDeceleration = 15f;
    private float noInputDecelDelay = 0.1f;
    private float deadZone = 0.2f;

    private float turnStartSpeedRatio = 0.85f;
    private float turnEndSpeedRatio = 0.55f;
    
    private bool snapToFullSpeed = true;

    private bool isTurning;
    private float noInputTime;

    public void Move(Entity mover, Rigidbody2D rb, Vector2 direction)
    {
        if (mover is not Player player)
        {
            Debug.LogError("PhysicsMovementStrategy: mover is not a Player");
            return;
        }

        float inputX = direction.x;
        float currentVelX = rb.linearVelocity.x;

        bool grounded = player.IsGrounded;
        float accel = grounded ? acceleration : airAcceleration;
        float decel = grounded ? deceleration : airDeceleration;

        if (Mathf.Abs(inputX) < deadZone)
        {
            isTurning = false;
            noInputTime += Time.fixedDeltaTime;
            if (noInputTime >= noInputDecelDelay)
            {
                float stoppedVelX = Mathf.MoveTowards(currentVelX, 0f, decel * Time.fixedDeltaTime);
                rb.linearVelocity = new Vector2(stoppedVelX, rb.linearVelocity.y);
            }
            return;
        }
        noInputTime = 0f;

        UpdateFacing(player, inputX);

        float targetVelX = snapToFullSpeed
            ? Mathf.Sign(inputX) * player.MoveSpeed
            : inputX * player.MoveSpeed;

        float defaultSpeed = player.GetDefaultStat<float>(StatType.MoveSpeed);
        float speedRatio = defaultSpeed > 0f ? Mathf.Abs(currentVelX) / defaultSpeed : 0f;
        bool isOpposing = currentVelX * inputX < 0f;
        if (!isTurning && isOpposing && speedRatio >= turnStartSpeedRatio)
        {
            isTurning = true;
            player.Machine.ChangeState<PlayerTurnState>();
        }
        if (isTurning && (!isOpposing || speedRatio <= turnEndSpeedRatio))
        {
            isTurning = false;
        }

        float rate = isTurning ? accel + decel : accel;
        float newVelX = Mathf.MoveTowards(currentVelX, targetVelX, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newVelX, rb.linearVelocity.y);
    }

    public void UpdateFacing(Entity mover, float inputX)
    {
        if (inputX == 0f || mover is not Player player)
            return;

        int flip = inputX > 0f ? 1 : -1;
        float scale = Mathf.Abs(player.transform.localScale.x);
        player.transform.localScale = new Vector3(scale * flip, scale, scale);
    }
}