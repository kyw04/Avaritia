using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Weapon")]
public class Weapon : ScriptableObject, IInventoryItem
{
    public AttackDataCombo combo;
    public StatData statBonusData;
    public Sprite icon;
    [TextArea] public string description;
    // ManualTrigger abilities here never fire — there's no "use weapon passive" input.
    // Fine to leave empty on ordinary weapons; only Event/Periodic triggers self-activate.
    public List<AbilityData> passiveAbilities = new();

    public string DisplayName => name;
    public string Details => description;
    public Sprite Icon => icon;

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
        return StatMath.Add(baseValue, bonus);
    }
}
