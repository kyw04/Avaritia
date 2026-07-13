using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Skill Data")]
public class SkillData : ScriptableObject
{
    public float cooldown;
    [SerializeReference, SubclassSelector] public List<ISkillEffect> effects = new();

    public void Activate(IAttacker caster, Transform target = null)
    {
        foreach (var effect in effects)
            effect?.Apply(caster, target);
    }
}
