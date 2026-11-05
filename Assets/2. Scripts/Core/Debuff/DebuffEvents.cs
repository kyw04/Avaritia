// Source = 디버프가 걸린 대상. IsNew가 false면 이미 걸린 디버프를 갱신한 것이다.
public struct DebuffAppliedEvent : ISubject
{
    public Entity Source { get; private set; }
    public Entity Inflictor { get; private set; }
    public ActiveDebuff Debuff { get; private set; }
    public bool IsNew { get; private set; }
    public DebuffAppliedEvent(Entity source, Entity inflictor, ActiveDebuff debuff, bool isNew)
    {
        Source = source; Inflictor = inflictor; Debuff = debuff; IsNew = isNew;
    }
}

public struct DebuffRemovedEvent : ISubject
{
    public Entity Source { get; private set; }
    public ActiveDebuff Debuff { get; private set; }
    public DebuffRemovedEvent(Entity source, ActiveDebuff debuff) { Source = source; Debuff = debuff; }
}
