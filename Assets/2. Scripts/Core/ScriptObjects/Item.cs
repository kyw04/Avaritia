using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Item")]
public class Item : ScriptableObject, IInventoryItem
{
    public Sprite icon;
    [TextArea] public string description;
    // Weapon.passiveAbilities와 동일한 이유로 ManualTrigger는 여기서도 무의미하다.
    public List<AbilityData> passiveAbilities = new();

    public string DisplayName => name;
    public string Details => description;
    public Sprite Icon => icon;
}
