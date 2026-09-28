using System.Collections.Generic;

public interface IBuffable
{
    // conditions: the buff only counts while all of them hold (checked on every stat read).
    // clearOnDamageTaken: the buff is removed the moment the owner actually takes damage.
    void ApplyBuff(object source, StatType type, BuffValueType valueType, float amount, float duration,
        List<IAbilityCondition> conditions = null, bool clearOnDamageTaken = false);
    float GetBuffAmount(object source, StatType type, BuffValueType valueType);
    void RemoveBuffsBySource(object source);
}
