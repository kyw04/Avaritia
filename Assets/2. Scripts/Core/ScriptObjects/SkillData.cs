using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Skill Data")]
public class SkillData : ScriptableObject, IInventoryItem
{
    public float cooldown;
    public float maxRange = float.MaxValue;
    public Sprite icon;
    [TextArea] public string description;
    [SerializeReference, SubclassSelector] public List<ISkillEffect> effects = new();

    public string DisplayName => name;
    public string Details => description;
    public Sprite Icon => icon;

    public void Activate(IAttacker caster, Transform target = null)
    {
        foreach (var effect in effects)
            effect?.Apply(caster, target);
    }
}
