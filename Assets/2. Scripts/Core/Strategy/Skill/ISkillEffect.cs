using UnityEngine;

public interface ISkillEffect
{
    void Apply(IAttacker caster, Transform target);
}
