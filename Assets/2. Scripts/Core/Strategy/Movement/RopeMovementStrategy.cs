using UnityEngine;

// 로프에 매달린 동안 쓰는 이동 전략. 공중에서는 좌우 입력으로 흔들고, 땅에서는 원래 이동 전략으로 걷는다.
// 위/아래 입력으로 줄을 감고 푼다. 줄 길이 제한은 DistanceJoint2D가 맡는다.
// 줄을 놓은 뒤(Detach)에도 착지할 때까지는 이 전략을 그대로 써서 공중 감속 없이 스윙 관성을 유지한다.
[System.Serializable]
public class RopeMovementStrategy : IMovementStrategy
{
    private DistanceJoint2D joint;
    private readonly IMovementStrategy groundStrategy;
    private readonly float swingForce;
    private readonly float pumpMultiplier;
    private readonly float maxSpeed;
    private readonly float reelSpeed;
    private readonly float minLength;
    private readonly float maxLength;

    public RopeMovementStrategy(DistanceJoint2D joint, IMovementStrategy groundStrategy, float swingForce, float pumpMultiplier,
        float maxSpeed, float reelSpeed, float minLength, float maxLength)
    {
        this.joint = joint;
        this.groundStrategy = groundStrategy;
        this.swingForce = swingForce;
        this.pumpMultiplier = pumpMultiplier;
        this.maxSpeed = maxSpeed;
        this.reelSpeed = reelSpeed;
        this.minLength = minLength;
        this.maxLength = maxLength;
    }

    public void Detach() => joint = null;

    public void Move(Entity mover, Rigidbody2D rb, Vector2 direction)
    {
        if (joint != null)
            joint.distance = Mathf.Clamp(joint.distance - direction.y * reelSpeed * Time.fixedDeltaTime, minLength, maxLength);

        if (mover.IsGrounded)
        {
            groundStrategy?.Move(mover, rb, direction);
            return;
        }

        UpdateFacing(mover, direction.x);

        // 흔들리는 방향으로 밀면(펌핑) 더 세게 가속하되, 최대 속도를 넘겨서까지 밀지는 않는다.
        bool withSwing = direction.x * rb.linearVelocity.x > 0f;
        if (withSwing && Mathf.Abs(rb.linearVelocity.x) >= maxSpeed)
            return;

        float force = direction.x * swingForce * (withSwing && joint != null ? pumpMultiplier : 1f);
        rb.AddForce(new Vector2(force, 0f));
    }

    public void UpdateFacing(Entity mover, float inputX) => groundStrategy?.UpdateFacing(mover, inputX);
}
