using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Ability Data")]
public class AbilityData : ScriptableObject, IInventoryItem
{
    public float cooldown;
    public float maxRange = float.MaxValue;
    public Sprite icon;
    public ActionFeedback cast;
    public string displayName;
    [TextArea] public string description;
    [SerializeReference, SubclassSelector] public IAbilityTrigger trigger;
    [SerializeReference, SubclassSelector] public List<IAbilityCondition> conditions = new();
    [SerializeReference, SubclassSelector] public List<IAbilityEffect> effects = new();
    // 이 Ability가 바인딩될 때 함께 바인딩/해제되는 Ability (예: 수동 스킬에 딸린 장착 중 패시브).
    public List<AbilityData> linkedAbilities = new();

    public Entity Owner { get; }
    public string DisplayName => displayName;
    public string Details => description;
    public Sprite Icon => icon;
}
