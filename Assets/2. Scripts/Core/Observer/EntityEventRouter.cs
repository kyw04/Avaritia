// 엔티티 이벤트는 발신 엔티티의 로컬 허브에 먼저 보내고, 전역 버스에도 보낸다.
// 자기 엔티티 이벤트만 듣는 컴포넌트(애니메이터, 피격 반응)는 로컬 허브를, 모든 엔티티를 듣는 매니저는 전역 버스를 구독한다.
// 전역 구독자가 없는 이벤트는 EventBus.Publish가 조회 한 번으로 끝난다.
public static class EntityEventRouter
{
    public static void Raise<T>(T e) where T : IEntityEvent
    {
        var source = e.Source;
        if (source != null)
            source.Events.Publish(e);
        EventBus.Publish(e);
    }
}
