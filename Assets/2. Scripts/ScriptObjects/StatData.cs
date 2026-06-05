using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatType
{
    None,
    Health,
    Speed,
    Damage,
    Armor,
}

public static class StatParameter
{
    private static readonly Dictionary<StatType, Type> StatDictionary = new()
    {
        { StatType.Health, typeof(float) },
        { StatType.Speed, typeof(float) },
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
    [Serializable] public abstract class StatValue { }
    [Serializable] public class IntValue : StatValue { public int value; }
    [Serializable] public class FloatValue : StatValue { public float value; }
    [Serializable] public class StringValue : StatValue { public string value; }

    
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
}
