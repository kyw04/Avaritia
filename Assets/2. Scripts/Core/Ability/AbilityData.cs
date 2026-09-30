using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Ability Data")]
public class AbilityData : ScriptableObject, IInventoryItem
{
    public float cooldown;
    public float maxRange = float.MaxValue;
    public Sprite icon;
    public string displayName;
    [TextArea] public string description;
    [SerializeReference, SubclassSelector] public IAbilityTrigger trigger;
    [SerializeReference, SubclassSelector] public List<IAbilityCondition> conditions = new();
    [SerializeReference, SubclassSelector] public List<IAbilityEffect> effects = new();

    public string DisplayName => displayName;
    public string Details => description;
    public Sprite Icon => icon;
}
