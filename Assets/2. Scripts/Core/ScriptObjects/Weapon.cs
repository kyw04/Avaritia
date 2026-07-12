using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Weapon")]
public class Weapon : ScriptableObject
{
    public AttackDataCombo combo;
    public StatData statBonusData;

    public bool TryGetStatBonus<T>(StatType type, out T value)
    {
        if (statBonusData == null)
        {
            value = default;
            return false;
        }

        return statBonusData.TryGetValue(type, out value);
    }

    public T ApplyBonus<T>(StatType type, T baseValue)
    {
        if (!TryGetStatBonus<T>(type, out var bonus)) return baseValue;
        return Add(baseValue, bonus);
    }

    private static T Add<T>(T a, T b)
    {
        if (typeof(T) == typeof(float)) return (T)(object)((float)(object)a + (float)(object)b);
        if (typeof(T) == typeof(int)) return (T)(object)((int)(object)a + (int)(object)b);
        return a;
    }
}
