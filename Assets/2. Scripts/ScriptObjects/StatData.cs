using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatType
{
    None,
    MaxHealth,
    Speed,
    JumpForce,
    Damage,
    Armor,
}

public static class StatParameter
{
    private static readonly Dictionary<StatType, Type> StatDictionary = new()
    {
        { StatType.MaxHealth, typeof(float) },
        { StatType.Speed, typeof(float) },
        { StatType.JumpForce, typeof(float) },
        { StatType.Damage, typeof(float) },
        { StatType.Armor, typeof(int) },
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
    [Serializable]
    public abstract class StatValue
    {
        public abstract object Value { get; }
        
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
    }

    [Serializable]
    public class IntValue : StatValue
    {
        public int value;
        public override object Value => value;
    }

    [Serializable]
    public class FloatValue : StatValue
    {
        public float value;
        public override object Value => value;
    }

    [Serializable]
    public class StringValue : StatValue
    {
        public string value;
        public override object Value => value;
    }


    [Serializable]
    public class StatEntry
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
                
                return default;
            }
        }
        
        return default;
    }
}
