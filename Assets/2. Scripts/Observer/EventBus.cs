using System;
using System.Collections.Generic;

public static class EventBus
{
    private static readonly Dictionary<Type, List<object>> observers = new();
    
    public static void Subscribe<T>(IObserver<T> observer) where T : ISubject
    {
        Type type = typeof(T);

        if (!observers.ContainsKey(type))
            observers[type] = new List<object>();

        if (!observers[type].Contains(observer))
            observers[type].Add(observer);
    }
    
    public static void Unsubscribe<T>(IObserver<T> observer)  where T : ISubject
    {
        Type type = typeof(T);

        if (!observers.TryGetValue(type, out var ob))
            return;

        ob.Remove(observer);
    }
    
    public static void Publish<T>(T gameEvent) where T : ISubject
    {
        Type type = typeof(T);

        if (!observers.TryGetValue(type, out var list))
            return;

        for (int i = 0; i < list.Count; i++)
        {
            ((IObserver<T>)list[i]).OnNotify(gameEvent);
        }
    }
}
