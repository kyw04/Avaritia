using UnityEngine;

public struct EntityIdleEvent : ISubject
{
    public Entity Source { get; private set; }
    public EntityIdleEvent(Entity source) { Source = source; }
}
public struct EntityFallingEvent : ISubject
{
    public Entity Source { get; private set; }
    public EntityFallingEvent(Entity source) { Source = source; }
}
public struct EntityLandedEvent : ISubject
{
    public Entity Source { get; private set; }
    public PooledParticle Particle { get; private set; }
    public EntityLandedEvent(Entity source, PooledParticle particle) { Source = source; Particle = particle; }
}
public struct EntityTurnEvent : ISubject
{
    public Entity Source { get; private set; }
    public PooledParticle Particle { get; private set; }
    public EntityTurnEvent(Entity source, PooledParticle particle) { Source = source; Particle = particle; }
}
// 턴 상태가 끝났을 때(미끄러지는 소리 루프를 멈추는 용도).
public struct EntityTurnEndedEvent : ISubject
{
    public Entity Source { get; private set; }
    public EntityTurnEndedEvent(Entity source) { Source = source; }
}
public struct EntityDashEvent : ISubject
{
    public Entity Source { get; private set; }
    public PooledParticle Particle { get; private set; }
    public EntityDashEvent(Entity source, PooledParticle particle) { Source = source; Particle = particle; }
}
public struct EntityJumpedEvent : ISubject
{
    public Entity Source { get; private set; }
    public JumpData Data { get; private set; }
    public EntityJumpedEvent(Entity source, JumpData data) { Source = source; Data = data; }
}
public struct EntityMovedEvent : ISubject
{
    public Entity Source { get; private set; }
    public float Speed { get; private set; }
    public EntityMovedEvent(Entity source, float speed) { Source = source; Speed = speed; }
}

public struct EntityAnimationEvent : ISubject
{
    public Entity Source { get; private set; }
    public AnimationClip Clip { get; private set; }
    public EntityAnimationEvent(Entity source, AnimationClip clip) { Source = source; Clip = clip; }
}

public struct EnemyConvertedEvent : ISubject
{
    public Enemy Source { get; private set; }
    public EnemyConvertedEvent(Enemy source) { Source = source; }
}

// 플레이어 사망. 사망 연출과 런 통계가 받는다.
public struct PlayerDiedEvent : ISubject
{
    public Player Player { get; private set; }
    public PlayerDiedEvent(Player player) { Player = player; }
}

public struct EntityDeadEvent : ISubject
{
    public Entity Source { get; private set; }
    public EntityDeadEvent(Entity source) { Source = source; }
}

// Source = the entity that took the damage. Attacker is null when unknown (effects).
// Amount는 방어력/실드를 거친 실제 피해량. 반응하는 쪽은 Info.Has(DamageFlags.X)로 무시할지 정한다.
public struct EntityDamagedEvent : ISubject
{
    public Entity Source { get; private set; }
    public DamageInfo Info { get; private set; }
    public Entity Attacker => Info.Attacker;
    public float Amount => Info.Amount;
    public Vector3? HitPoint => Info.HitPoint;
    public bool IsCritical => Info.IsCritical;
    public EntityDamagedEvent(Entity source, DamageInfo info) { Source = source; Info = info; }
}

// Published when Source dodged an attack via the Evasion stat (no damage was applied).
public struct EntityEvadedEvent : ISubject
{
    public Entity Source { get; private set; }
    public Entity Attacker { get; private set; }
    public Vector3? HitPoint { get; private set; }
    public EntityEvadedEvent(Entity source, Entity attacker, Vector3? hitPoint = null) { Source = source; Attacker = attacker; HitPoint = hitPoint; }
}

// Published when Source nullified an attack by consuming a hit block (no damage was applied).
public struct EntityHitBlockedEvent : ISubject
{
    public Entity Source { get; private set; }
    public Entity Attacker { get; private set; }
    public Vector3? HitPoint { get; private set; }
    public EntityHitBlockedEvent(Entity source, Entity attacker, Vector3? hitPoint = null) { Source = source; Attacker = attacker; HitPoint = hitPoint; }
}

// Published when Source's HP hits 0, before it dies — a listener that heals it cancels the death.
public struct EntityLethalDamageEvent : ISubject
{
    public Entity Source { get; private set; }
    public EntityLethalDamageEvent(Entity source) { Source = source; }
}

public struct EntityAttackEvent : ISubject
{
    public AttackData Data { get; private set; }
    public EntityAttackEvent(AttackData data)  { Data = data; }
}

public struct EntityAttackStartEvent : ISubject
{
    public Entity Source { get; private set; }
    public AttackData Data { get; private set; }
    public EntityAttackStartEvent(Entity source, AttackData data)  { Source = source; Data = data; }
}

public struct EntityAttackCanceledEvent : ISubject
{
    public Entity Source { get; private set; }
    public EntityAttackCanceledEvent(Entity source) { Source = source; }
}

public struct EntityAttackBufferEvent : ISubject { }

public struct EntityAttackEndEvent : ISubject { }

public struct EntityHealthChangedEvent : ISubject
{
    public Entity Source { get; private set; }
    public float Max { get; private set; }
    public float Current { get; private set; }
    public EntityHealthChangedEvent(Entity source, float max, float current) { Source = source; Max = max; Current = current; }
}

public struct EntityDashCountChangedEvent : ISubject
{
    public Entity Source { get; private set; }
    public int Current { get; private set; }
    public int Max { get; private set; }
    public float StartTime { get; private set; }
    public EntityDashCountChangedEvent(Entity source, int current, int max, float startTime) 
    {
        Source = source;
        Current = current;
        Max = max;
        StartTime = startTime;
    }
}

public struct EntitySkillCooldownEvent : ISubject
{
    public Entity Source { get; private set; }
    public int SlotIndex { get; private set; }
    public float Duration { get; private set; }
    public float EndTime { get; private set; }
    public EntitySkillCooldownEvent(Entity source, int slotIndex, float duration, float endTime)
    {
        Source = source; SlotIndex = slotIndex; Duration = duration; EndTime = endTime;
    }
}

public struct EntitySkillEquippedEvent : ISubject
{
    public Entity Source { get; private set; }
    public int SlotIndex { get; private set; }
    public AbilityData Skill { get; private set; }
    public EntitySkillEquippedEvent(Entity source, int slotIndex, AbilityData skill)
    {
        Source = source; SlotIndex = slotIndex; Skill = skill;
    }
}

// 플레이어가 픽업으로 무기/스킬/아이템을 실제로 장착했을 때(아이템 교체는 확인 시점).
public struct PlayerEquippedEvent : ISubject
{
    public Player Player { get; private set; }
    public PlayerEquippedEvent(Player player) { Player = player; }
}
