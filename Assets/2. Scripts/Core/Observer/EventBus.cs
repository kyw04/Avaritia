using System;
using System.Collections.Generic;
using Unity.Profiling;

public static class EventBus
{
    private static readonly ProfilerMarker PublishMarker = new("EventBus.Publish");
    // 측정용: 전역 버스와 엔티티 로컬 허브가 부른 OnNotify 누적 횟수. EventDispatchBenchmark가 프레임 차이로 읽는다.
    public static int NotifyCount;

    private static readonly Dictionary<Type, List<object>> observers = new();
    // 구독자별로 구독한 이벤트 타입을 기억해 UnsubscribeAll이 관계없는 리스트까지 훑지 않게 한다.
    private static readonly Dictionary<object, List<Type>> subscriptions = new();
    // 풀에서 꺼낸 객체가 켜질 때마다 타입 리스트를 새로 만들지 않도록 다 쓴 리스트를 모아 재사용한다.
    private static readonly Stack<List<Type>> freeTypeLists = new();

    public static void Subscribe<T>(IObserver<T> observer) where T : ISubject
    {
        Type type = typeof(T);

        if (!subscriptions.TryGetValue(observer, out var types))
            subscriptions[observer] = types = freeTypeLists.Count > 0 ? freeTypeLists.Pop() : new List<Type>();
        else if (types.Contains(type))
            return;

        types.Add(type);

        if (!observers.TryGetValue(type, out var list))
            observers[type] = list = new List<object>();

        list.Add(observer);
    }

    public static void Unsubscribe<T>(IObserver<T> observer)  where T : ISubject
    {
        Type type = typeof(T);

        if (!subscriptions.TryGetValue(observer, out var types) || !types.Remove(type))
            return;

        if (types.Count == 0)
        {
            subscriptions.Remove(observer);
            freeTypeLists.Push(types);
        }

        RemoveByReference(observers[type], observer);
    }

    public static void Publish<T>(T gameEvent) where T : ISubject
    {
        Type type = typeof(T);

        if (!observers.TryGetValue(type, out var list))
            return;

        using (PublishMarker.Auto())
        {
            NotifyCount += list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                ((IObserver<T>)list[i]).OnNotify(gameEvent);
            }
        }
    }

    public static void UnsubscribeAll(object observer)
    {
        if (!subscriptions.Remove(observer, out var types))
            return;

        foreach (var type in types)
            RemoveByReference(observers[type], observer);

        types.Clear();
        freeTypeLists.Push(types);
    }

    // List.Remove는 UnityEngine.Object.Equals(네이티브 호출)로 비교해 구독자가 많으면 느리다. 참조로만 비교한다.
    private static void RemoveByReference(List<object> list, object observer)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (ReferenceEquals(list[i], observer))
            {
                list.RemoveAt(i);
                return;
            }
        }
    }
}
