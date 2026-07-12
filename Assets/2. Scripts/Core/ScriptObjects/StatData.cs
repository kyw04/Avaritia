using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatType
{
    None,
    MaxHealth,
    CurrentHealth,
    MoveSpeed,
    DoubleJumpCount,
    JumpForce,
    Damage,
    Armor,
    DashCount,
    DashCooldown,
}

public static class StatParameter
{
    private static readonly Dictionary<StatType, Type> StatDictionary = new()
    {
        { StatType.MaxHealth, typeof(float) },
        { StatType.CurrentHealth, typeof(float) },
        { StatType.MoveSpeed, typeof(float) },
        { StatType.DoubleJumpCount, typeof(int) },
        { StatType.JumpForce, typeof(float) },
        { StatType.Damage, typeof(float) },
        { StatType.Armor, typeof(int) },
        { StatType.DashCount, typeof(int) },
        { StatType.DashCooldown, typeof(float) },
    };

    public static Type GetStatParameter(StatType type)
    {
        StatDictionary.TryGetValue(type, out var result);
        return result;
    }
}

[CreateAssetMenu(fileName = "StatData", menuName = "Scriptable Objects/StatData", order = 1)]
public class StatData : ScriptableObject
{
    [Serializable] public abstract class StatValue
    {
        public abstract object Value { get; internal set; }
        
        public bool TryGetValue<T>(out T result)
        {
            if (Value is T t)
            {
                result = t;
                return true;
            }

            result = default;
            return false;
        }

        public bool TrySetValue<T>(T value)
        {
            if (Value is not T)
                return false;
            
            Value = value;
            return true;

        }
    }
    [Serializable] public class IntValue : StatValue
    {
        public int value;

        public override object Value
        {
            get => value;
            internal set => this.value = (int)value;
        }
    }
    [Serializable] public class FloatValue : StatValue
    {
        public float value;
        public override object Value
        {
            get => value;
            internal set => this.value = (float)value;
        }
    }
    [Serializable] public class StringValue : StatValue
    {
        public string value;
        public override object Value
        {
            get => value;
            internal set => this.value = (string)value;
        }
    }
    
    [Serializable] public class StatEntry
    {
        [SerializeReference] public StatType statType;
        [SerializeReference] public StatValue value;

        public StatEntry(StatType statType, StatValue value)
        {
            this.statType = statType;
            this.value = value;
        }
    }

    [SerializeReference, HideInInspector] public List<StatEntry> stats = new();

    public T TryGetValue<T>(StatType statType)
    {
        foreach (var stat in stats)
        {
            if (stat.statType == statType)
            {
                if (stat.value.TryGetValue<T>(out var r))
                    return r;

                Debug.LogError($"Type error for type {statType}");
                return default;
            }
        }

        Debug.LogError($"No stat found for type {statType}");
        return default;
    }

    public bool TryGetValue<T>(StatType statType, out T result)
    {
        foreach (var stat in stats)
        {
            if (stat.statType == statType)
                return stat.value.TryGetValue(out result);
        }

        result = default;
        return false;
    }

    public void SetValue<T>(StatType statType, T value = default)
    {
        foreach (var stat in stats)
        {
            if (stat.statType == statType)
            {
                if (stat.value.TrySetValue(value))
                    return;
                
                Debug.LogError($"Type error for type {statType}");
                return;
            }
        }
        
        Debug.LogError($"No stat found for type {statType}");
    }
}
