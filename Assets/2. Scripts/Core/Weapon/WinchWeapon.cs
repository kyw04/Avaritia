using System.Collections;
using UnityEngine;

// 원치 무기: 기본 공격은 근접 콤보, 방향키(8방향)를 누른 채 공격하면 그 방향으로 갈고리를 날린다.
// 가벼운 적(일반 크기 Enemy)은 맞히면 피해를 주고 플레이어 앞으로 끌어오고,
// 무거운 적(대형 Enemy, 보스 등)은 플레이어가 그 적에게 날아가 도착하면 공격한다. 피해를 줄 때 Enemy는 스턴된다.
// 벽/바닥에 걸리면 로프에 매달려 흔들 수 있고, 점프 입력을 넣거나 공중에서 발이 땅에 닿으면 풀린다.
// 끌려오거나 날아가는 동안에는 넉백 상태로 만들어 상태 머신의 이동이 속도를 덮어쓰지 않게 한다.
[CreateAssetMenu(menuName = "Scriptable Objects/Weapon/WinchWeapon")]
public class WinchWeapon : InputWeapon
{
    [SerializeField] private LineRenderer ropePrefab;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private Vector2 originOffset;
    [SerializeField, Min(0)] private float hookRadius = 0.25f;
    [SerializeField, Min(0)] private float hookRange = 6f;
    [SerializeField, Min(0)] private float hookSpeed = 30f;
    [SerializeField, Min(0)] private float maxReelTime = 0.6f;
    [SerializeField, Min(0)] private float hookCooldown = 1f;
    [SerializeField, Min(0)] private float pullDamageMultiplier = 1f;
    [SerializeField, Min(0)] private float pullSpeed = 18f;
    [SerializeField, Min(0)] private float pullStopDistance = 1.2f;
    [SerializeField, Min(0)] private float flyDamageMultiplier = 1.5f;
    [SerializeField, Min(0)] private float flySpeed = 20f;
    [SerializeField, Min(0)] private float flyStopDistance = 0.6f;
    [SerializeField, Min(0)] private float stunDuration = 1.5f;
    [SerializeField, Min(0)] private float swingForce = 15f;
    [SerializeField, Min(1)] private float pumpMultiplier = 2f;
    [SerializeField, Min(0)] private float maxSwingSpeed = 12f;
    [SerializeField, Min(0)] private float reelSpeed = 4f;
    [SerializeField, Min(0)] private float minRopeLength = 1f;

    private LineRenderer rope;
    private Coroutine hookRoutine;
    private float savedAttackReadyTime;
    private float savedGravityScale;
    private bool isFlying;
    private float hookReadyTime;
    private DistanceJoint2D ropeJoint;
    private RopeMovementStrategy ropeMovement;
    // 로프 이동으로 바꿔둔 동안(줄을 놓고 착지하기 전까지 포함)만 원래 이동 전략을 들고 있다.
    private IMovementStrategy savedMovementStrategy;
    private Coroutine landRoutine;

    private Vector2 Origin => (Vector2)Owner.transform.position + new Vector2(originOffset.x * Owner.LookDirection, originOffset.y);

    public override void Equip(Entity owner, ImageCountBar countBar = null)
    {
        base.Equip(owner, countBar);
        rope = Instantiate(ropePrefab, owner.transform);
        rope.enabled = false;
    }

    public override void Unequip()
    {
        if (hookRoutine != null)
        {
            Owner.StopCoroutine(hookRoutine);
            EndHook();
        }
        StopLandRoutine();
        RestoreMovement();
        if (rope != null)
            Destroy(rope.gameObject);
        base.Unequip();
    }

    // Attack 액션의 Performed에 연결한다. 방향키를 누르지 않았거나 쿨타임 중이면 근접 공격이 그대로 나간다.
    // 갈고리를 쓰는 동안에는 공격 입력이 들어와도 근접 공격이 나가지 않는다.
    [InputMethod]
    private void FireHook()
    {
        if (hookRoutine != null || Owner is not Player player || Owner.IsAttacking || Time.time < hookReadyTime)
            return;
        if (!TryGetHookDirection(InputHandler.Instance.MoveInput, out var direction))
            return;

        // InputHandler는 같은 공격 입력으로 Update에서 근접 공격을 시작하려 하므로, 끝날 때까지 CanAttack을 막아둔다.
        savedAttackReadyTime = player.AttackReadyTime;
        player.AttackReadyTime = float.PositiveInfinity;
        Owner.UpdateFacing(direction);
        hookRoutine = Owner.StartCoroutine(HookRoutine(direction));
    }

    // 입력을 위/아래/앞/뒤와 대각선의 8방향으로 맞춘다.
    private static bool TryGetHookDirection(Vector2 input, out Vector2 direction)
    {
        const float threshold = 0.5f;
        float x = Mathf.Abs(input.x) >= threshold ? Mathf.Sign(input.x) : 0f;
        float y = Mathf.Abs(input.y) >= threshold ? Mathf.Sign(input.y) : 0f;
        direction = new Vector2(x, y).normalized;
        return direction != Vector2.zero;
    }

    private IEnumerator HookRoutine(Vector2 direction)
    {
        rope.enabled = true;
        Vector2 tip = Origin;
        SetRope(tip);

        RaycastHit2D hit = default;
        for (float traveled = 0f; traveled < hookRange && !hit; )
        {
            yield return new WaitForFixedUpdate();
            float step = Mathf.Min(hookSpeed * Time.fixedDeltaTime, hookRange - traveled);
            hit = Physics2D.CircleCast(tip, hookRadius, direction, step, targetLayer | obstacleLayer);
            tip = hit ? hit.centroid : tip + direction * step;
            traveled += step;
            SetRope(tip);
        }

        int hitLayer = hit ? 1 << hit.collider.gameObject.layer : 0;
        var target = (targetLayer & hitLayer) != 0 ? hit.collider.GetComponentInParent<Entity>() : null;

        // 빗나가거나 벽에 맞으면 쿨타임 없이 바로 다시 쓸 수 있다.
        if (target != null)
            hookReadyTime = Time.time + hookCooldown;

        if (target is Enemy { Size: EnemySize.Normal })
            yield return Pull(target);
        else if (target != null)
            yield return Fly(target, hit.collider);
        // 아래로 쏴서 바닥에 걸린 줄은 매달릴 수 없으니 회수한다.
        else if ((obstacleLayer & hitLayer) != 0 && direction.y >= 0f)
            yield return Swing(hit.point);
        else
            yield return Retract(tip);

        EndHook();
    }

    // 맞히는 순간 피해를 주고(무기 넉백이 걸린다) 바로 다음 물리 프레임부터 끌어오는 속도로 덮어쓴다.
    private IEnumerator Pull(Entity target)
    {
        Hit(target, pullDamageMultiplier);

        for (float elapsed = 0f; elapsed < maxReelTime && IsAlive(target); elapsed += Time.fixedDeltaTime)
        {
            Vector2 toOwner = Origin - (Vector2)target.transform.position;
            if (toOwner.magnitude <= pullStopDistance)
                break;

            Hold(target, toOwner.normalized * pullSpeed);
            SetRope(target.transform.position);
            yield return new WaitForFixedUpdate();
        }

        if (IsAlive(target))
            Hold(target, Vector2.zero);
    }

    // 큰 적은 중심이 멀어 콜라이더 표면(가장 가까운 점)을 향해 날아가고, 거기에 닿으면 공격한다.
    private IEnumerator Fly(Entity target, Collider2D targetCollider)
    {
        savedGravityScale = Owner.Rb.gravityScale;
        Owner.Rb.gravityScale = 0f;
        isFlying = true;

        for (float elapsed = 0f; elapsed < maxReelTime && IsAlive(target); elapsed += Time.fixedDeltaTime)
        {
            Vector2 point = targetCollider.ClosestPoint(Origin);
            Vector2 toTarget = point - Origin;
            if (toTarget.magnitude <= flyStopDistance)
            {
                Hit(target, flyDamageMultiplier);
                break;
            }

            Owner.UpdateFacing(toTarget);
            Hold(Owner, toTarget.normalized * flySpeed);
            SetRope(point);
            yield return new WaitForFixedUpdate();
        }

        Hold(Owner, Vector2.zero);
    }

    // 걸린 지점까지의 거리를 넘지 않게 DistanceJoint2D로 묶고, 풀릴 때까지 이동 전략을 로프 이동으로 바꾼다.
    private IEnumerator Swing(Vector2 anchor)
    {
        ropeJoint = Owner.gameObject.AddComponent<DistanceJoint2D>();
        ropeJoint.autoConfigureConnectedAnchor = false;
        ropeJoint.connectedAnchor = anchor;
        ropeJoint.autoConfigureDistance = false;
        ropeJoint.distance = Vector2.Distance(Owner.Rb.position, anchor);
        ropeJoint.maxDistanceOnly = true;
        ropeJoint.enableCollision = true;

        // 줄을 놓고 착지하기 전에 다시 걸면 원래 전략은 이미 들고 있으니 그대로 둔다.
        StopLandRoutine();
        savedMovementStrategy ??= Owner.MovementStrategy;
        ropeMovement = new RopeMovementStrategy(ropeJoint, savedMovementStrategy, swingForce, pumpMultiplier, maxSwingSpeed,
            reelSpeed, minRopeLength, hookRange);
        Owner.MovementStrategy = ropeMovement;

        // 땅에서 건 줄은 걸어 다닐 수 있지만, 공중에 떴다가 발이 닿으면 끊는다.
        bool wasGrounded = Owner.IsGrounded;
        while (ropeJoint != null)
        {
            if (Owner.IsGrounded && !wasGrounded)
            {
                DetachRope();
                break;
            }
            wasGrounded = Owner.IsGrounded;
            SetRope(anchor);
            yield return null;
        }
    }

    // 로프에 매달려 있으면 점프 입력은 점프 대신 줄을 놓는 데 쓴다.
    public override bool ConsumeJump()
    {
        if (ropeJoint == null)
            return false;

        DetachRope();
        return true;
    }

    // 공중에서 놓으면 착지할 때까지 로프 이동 전략을 유지해 스윙 관성을 살린다.
    private void DetachRope()
    {
        if (ropeJoint == null)
            return;

        // Destroy는 프레임 끝에 처리되므로 그 전에 물리가 돌아도 줄이 당기지 않게 바로 끈다.
        ropeJoint.enabled = false;
        Destroy(ropeJoint);
        ropeJoint = null;
        ropeMovement.Detach();
        ropeMovement = null;

        if (Owner.IsGrounded)
            RestoreMovement();
        else
            landRoutine = Owner.StartCoroutine(RestoreMovementOnLand());
    }

    private IEnumerator RestoreMovementOnLand()
    {
        yield return new WaitUntil(() => Owner.IsGrounded);
        landRoutine = null;
        RestoreMovement();
    }

    private void StopLandRoutine()
    {
        if (landRoutine == null)
            return;

        Owner.StopCoroutine(landRoutine);
        landRoutine = null;
    }

    private void RestoreMovement()
    {
        if (savedMovementStrategy == null)
            return;

        Owner.MovementStrategy = savedMovementStrategy;
        savedMovementStrategy = null;
    }

    private IEnumerator Retract(Vector2 tip)
    {
        while (tip != Origin)
        {
            yield return new WaitForFixedUpdate();
            tip = Vector2.MoveTowards(tip, Origin, hookSpeed * Time.fixedDeltaTime);
            SetRope(tip);
        }
    }

    // 다음 물리 프레임까지 넉백 상태로 두어 이동(상태 머신, 적 AI)이 이 속도를 덮어쓰지 못하게 한다.
    private static void Hold(Entity entity, Vector2 velocity)
    {
        entity.ApplyKnockback(Vector2.zero, Time.fixedDeltaTime * 2f);
        entity.Rb.linearVelocity = velocity;
    }

    private void Hit(Entity target, float multiplier)
    {
        bool isCritical = Random.Range(0f, 100f) < Owner.GetStat<float>(StatType.CritRate);
        float damage = Owner.GetDamageAgainst(target.transform) * multiplier * (isCritical ? CritConfig.Multiplier : 1f);
        target.TakeDamage(damage, Owner, target.transform.position, isCritical);

        // 스턴 상태는 넉백 중에는 속도를 건드리지 않아 끌려오는 동안에도 걸 수 있다.
        if (target is Enemy enemy && IsAlive(enemy))
            enemy.Stun(stunDuration);
    }

    private static bool IsAlive(Entity entity) => entity != null && entity.isActiveAndEnabled && entity.CurrentHealth > 0;

    private void SetRope(Vector2 end)
    {
        rope.SetPosition(0, Origin);
        rope.SetPosition(1, end);
    }

    private void EndHook()
    {
        hookRoutine = null;
        rope.enabled = false;
        DetachRope();
        if (isFlying)
        {
            Owner.Rb.gravityScale = savedGravityScale;
            isFlying = false;
        }
        if (Owner is Player player)
            player.AttackReadyTime = savedAttackReadyTime;
    }
}
