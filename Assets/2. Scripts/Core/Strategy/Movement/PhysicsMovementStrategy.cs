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

        // 공중에서 벽 쪽으로 계속 밀면 벽과의 마찰로 떨어지지 않고 붙어 버리므로, 그 방향 입력은 속도에 반영하지 않는다.
        if (!grounded && IsTouchingWall(rb, inputX))
            return;

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

    private static readonly ContactPoint2D[] contacts = new ContactPoint2D[8];

    // 입력 방향(inputX 부호) 쪽에 거의 수직인 벽 접촉이 있는지. 벽 법선은 플레이어 쪽을 향하므로 입력과 부호가 반대다.
    private static bool IsTouchingWall(Rigidbody2D rb, float inputX)
    {
        int count = rb.GetContacts(contacts);
        for (int i = 0; i < count; i++)
        {
            if (contacts[i].normal.x * Mathf.Sign(inputX) < -0.7f)
                return true;
        }
        return false;
    }

    public void UpdateFacing(Entity mover, float inputX)
    {
        if (inputX == 0f || mover is not Player player || !player.Restrictions.Can(ActionBlock.Turn))
            return;

        int flip = inputX > 0f ? 1 : -1;
        float scale = Mathf.Abs(player.transform.localScale.x);
        player.transform.localScale = new Vector3(scale * flip, scale, scale);
    }
}