public interface ISubject
{
}

// 발신 엔티티가 있는 이벤트. EntityEventRouter가 발신 엔티티의 로컬 허브로 보낼 때 쓴다.
public interface IEntityEvent : ISubject
{
    Entity Source { get; }
}
