using UnityEngine;

[System.Serializable]
public class AttackEffect : ISkillEffect
{
    public AttackData attackData;

    public void Apply(IAttacker caster, Transform target)
    {
        if (attackData == null) return;
        if (caster is Entity entity)
            EventBus.Publish(new EntityAttackStartEvent(entity, attackData));
        attackData.Attack(caster, target);
    }
}
