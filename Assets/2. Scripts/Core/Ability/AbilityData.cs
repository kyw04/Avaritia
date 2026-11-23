using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Ability Data")]
public class AbilityData : ScriptableObject, IInventoryItem, ISaveId
{
    [SerializeField, HideInInspector] private string saveId;
    public string SaveId => saveId;

    private void OnValidate()
    {
#if UNITY_EDITOR
        SaveIdUtility.Sync(this, ref saveId);
#endif
    }

    public float cooldown;
    // 대상이 이보다 가까우면 적은 뒤로 물러난 뒤 공격한다.
    public float minRange;
    public float maxRange = float.MaxValue;
    public RankData rank;
    public ActionFeedback cast;
    [SerializeField] private DisplayInfo info = new();
    [SerializeReference, SubclassSelector] public IAbilityTrigger trigger;
    [SerializeReference, SubclassSelector] public List<IAbilityCondition> conditions = new();
    [SerializeReference, SubclassSelector] public List<IAbilityEffect> effects = new();
    // 이 Ability가 바인딩될 때 함께 바인딩/해제되는 Ability (예: 수동 스킬에 딸린 장착 중 패시브).
    public List<AbilityData> linkedAbilities = new();

    public Entity Owner { get; }
    public string DisplayName => info.DisplayName;
    public string Details => info.Details;
    public Sprite Icon => info.Icon;
}
