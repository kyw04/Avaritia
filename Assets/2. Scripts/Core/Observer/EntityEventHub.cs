using System;
using System.Collections.Generic;
using Unity.Profiling;

// 엔티티 하나 안에서만 도는 이벤트 허브. 구독자가 자기 엔티티의 컴포넌트 몇 개뿐이라 리스트가 짧다.
public class EntityEventHub
{
    private static readonly ProfilerMarker PublishMarker = new("EntityEventHub.Publish");

    private readonly Dictionary<Type, List<object>> observers = new();

    public void Subscribe<T>(IObserver<T> observer) where T : IEntityEvent
    {
        if (!observers.TryGetValue(typeof(T), out var list))
            observers[typeof(T)] = list = new List<object>(2);
        for (int i = 0; i < list.Count; i++)
            if (ReferenceEquals(list[i], observer)) return;
        list.Add(observer);
    }

    // List.Remove는 UnityEngine.Object.Equals(네이티브 호출)로 비교하므로 EventBus와 같이 참조로만 비교한다.
    public void Unsubscribe<T>(IObserver<T> observer) where T : IEntityEvent
    {
        if (!observers.TryGetValue(typeof(T), out var list)) return;
        for (int i = 0; i < list.Count; i++)
        {
            if (ReferenceEquals(list[i], observer))
            {
                list.RemoveAt(i);
                return;
            }
        }
    }

    public void Publish<T>(T e) where T : IEntityEvent
    {
        if (!observers.TryGetValue(typeof(T), out var list)) return;
        using (PublishMarker.Auto())
        {
            EventBus.NotifyCount += list.Count;
            for (int i = 0; i < list.Count; i++)
                ((IObserver<T>)list[i]).OnNotify(e);
        }
    }
}
