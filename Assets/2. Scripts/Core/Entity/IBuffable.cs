public interface IBuffable
{
    void ApplyBuff(object source, StatType type, BuffValueType valueType, float amount, float duration);
}
