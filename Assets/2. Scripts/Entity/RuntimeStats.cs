using System.Collections.Generic;
using UnityEngine;

public class RuntimeStats
{
    private readonly Dictionary<StatType, object> _values = new();

    public RuntimeStats(StatData source)
    {
        foreach (var stat in source.stats)
        {
            _values[stat.statType] = stat.value.Value;
        }
    }

    public bool TryGet<T>(StatType type, out T result)
    {
        if (_values.TryGetValue(type, out var v) && v is T t)
        {
            result = t;
            return true;
        }
        result = default;
        return false;
    }

    public T Get<T>(StatType type)
    {
        if (TryGet<T>(type, out var r)) return r;
        Debug.LogError($"Stat not found or type mismatch: {type} as {typeof(T)}");
        return default;
    }

    public void Set<T>(StatType type, T value)
    {
        if (!_values.TryGetValue(type, out var current))
        {
            Debug.LogError($"No stat found for type {type}");
            return;
        }
        
        if (current.GetType() != typeof(T))
        {
            Debug.LogError($"Type error for type {type}: expected {current.GetType()}, got {typeof(T)}");
            return;
        }
        _values[type] = value;
    }
}