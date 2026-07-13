using UnityEngine;

[System.Serializable]
public class AttackEffect : ISkillEffect
{
    public AttackData attackData;

    public void Apply(IAttacker caster, Transform target)
    {
        attackData?.Attack(caster, target);
    }
}
