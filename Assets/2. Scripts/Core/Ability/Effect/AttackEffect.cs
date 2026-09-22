[System.Serializable]
public class AttackEffect : IAbilityEffect
{
    public AttackData attackData;

    public void Apply(AbilityContext context)
    {
        if (attackData == null) return;
        if (context.Caster is Entity entity)
            EventBus.Publish(new EntityAttackStartEvent(entity, attackData));
        attackData.Attack(context.Caster, context.Target);
    }
}
