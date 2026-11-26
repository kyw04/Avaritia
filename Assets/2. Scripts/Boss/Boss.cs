using System.Collections;
using UnityEngine;

public class Boss : Entity
{
    public IAIController Machine { get; private set; }
    public Transform Target { get; private set; }
    private StateManager stateManager;

    // 대상과 x 차이가 이 안이면(머리 위 등) 다가가지 않고 멈춘다.
    private const float ChaseStopDistance = 0.5f;
    // 뒷걸음질할 때의 이동 속도 배율
    private const float RetreatSpeedMultiplier = 0.5f;
    // 이 속도보다 느리면 걷기 대신 대기 애니메이션을 튼다.
    private const float MoveAnimationSpeed = 0.05f;

    // Machine is built here too (not just OnSpawn) so a directly scene-placed
    // (non-pooled) instance still gets a working state machine — OnSpawn only
    // fires for pool-spawned instances. OnSpawn unregisters and rebuilds it
    // again on every pool (re)spawn.
    protected override void Awake()
    {
        base.Awake();
        stateManager = StateManager.Instance;

        var player = FindAnyObjectByType<Player>();
        if (player != null)
            Target = player.transform;

        Machine = new BossBehaviorTree(this);
        Machine.Init();
    }

    // 스폰(보스방 입장)과 풀 반납(사망 모션 종료·방 이동·다시 시작)을 알려 보스 체력 UI가 켜지고 꺼지게 한다.
    public override void OnSpawn()
    {
        base.OnSpawn();
        if (Machine != null) stateManager.Unregister(Machine);
        var player = FindAnyObjectByType<Player>();
        if (player != null)
            Target = player.transform;

        Rb.simulated = true;
        Machine = new BossBehaviorTree(this);
        Machine.Init();
        EventBus.Publish(new BossSpawnedEvent(this));
    }

    public override void OnDespawn()
    {
        EventBus.Publish(new BossDespawnedEvent(this));
        base.OnDespawn();
    }

    // 공격 스킬 사거리 안이면 멈춰 대상을 바라보고, 모든 공격 스킬의 minRange보다 가까우면 뒤로 물러나고, 아니면 다가간다.
    public void MoveToTarget()
    {
        if (Target == null)
            return;

        int toTarget = FlipToTarget();
        float distance = Vector2.Distance(transform.position, Target.position);

        bool hasAttack = false;
        bool inRange = false;
        float minRange = float.MaxValue;
        foreach (var ability in abilities)
        {
            if (ability == null || ability.trigger is not ManualTrigger)
                continue;
            hasAttack = true;
            minRange = Mathf.Min(minRange, ability.minRange);
            if (distance >= ability.minRange && distance <= ability.maxRange)
                inRange = true;
        }

        if (hasAttack && inRange)
            Move(Vector2.zero);
        else if (hasAttack && distance < minRange)
            Move(new Vector2(-toTarget * RetreatSpeedMultiplier, 0));
        else if (Mathf.Abs(Target.position.x - transform.position.x) > ChaseStopDistance)
            Move(new Vector2(toTarget, 0));
        else
            Move(Vector2.zero);
    }

    // 공격을 시작할 때 이동 속도가 남아 미끄러지지 않게 멈춘다.
    public void StopMoving() => Move(Vector2.zero);

    public void PublishLocomotionAnimation()
    {
        float speed = Mathf.Abs(Rb.linearVelocityX);
        if (speed > MoveAnimationSpeed)
            EntityEventRouter.Raise(new EntityMovedEvent(this, speed));
        else
            EntityEventRouter.Raise(new EntityIdleEvent(this));
    }

    public int FlipToTarget()
    {
        int flip = Target.position.x > transform.position.x ? 1 : -1;
        float scale = Mathf.Abs(transform.localScale.x);
        transform.localScale = new Vector3(scale * flip, scale, scale);
        return flip;
    }

    // 진행 중인 행동을 멈추고 사망 모션(Feedbacks.Death)을 튼 뒤, 모션이 끝나면 풀에 돌려보낸다.
    // 사망 모션 동안 공격 판정에 걸리지 않게 물리에서 뺀다(OnSpawn에서 되돌린다).
    public override void Die()
    {
        if (!TryMarkDead()) return;
        stateManager.Unregister(Machine);
        CancelAttack();
        CancelChannels();
        Restrictions.Set(this, EnemyHitState.AllActions);
        Rb.linearVelocity = Vector2.zero;
        Rb.simulated = false;
        Debug.Log("Boss: 사망");

        var feedback = Feedbacks.Death;
        feedback.Play(FootPosition);
        if (feedback.animClip != null)
        {
            EntityEventRouter.Raise(new EntityAnimationEvent(this, feedback.animClip));
            StartCoroutine(DespawnAfter(feedback.animClip.length));
        }
        else
            ObjectPoolManager.Instance.Despawn(gameObject);
    }

    // 그 전에 풀에 먼저 돌아가면(방 이동 등) 비활성화로 코루틴도 멈춘다.
    private IEnumerator DespawnAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        ObjectPoolManager.Instance.Despawn(gameObject);
    }

    private void OnDestroy()
    {
        stateManager.Unregister(Machine);
    }
}
