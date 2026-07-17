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
    public EntityLandedEvent(Entity source) { Source = source; }
}
public struct EntityTurnEvent : ISubject
{
    public Entity Source { get; private set; }
    public EntityTurnEvent(Entity source) { Source = source; }
}
public struct EntityDashEvent : ISubject
{
    public Entity Source { get; private set; }
    public EntityDashEvent(Entity source) { Source = source; }
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

public struct EntityDeadEvent : ISubject
{
    public Entity Source { get; private set; }
    public EntityDeadEvent(Entity source) { Source = source; }
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
    public EntityDashCountChangedEvent(Entity source, int current, int max) { Source = source; Current = current; Max = max; }
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
