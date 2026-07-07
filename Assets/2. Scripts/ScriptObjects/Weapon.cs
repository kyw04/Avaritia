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
}
