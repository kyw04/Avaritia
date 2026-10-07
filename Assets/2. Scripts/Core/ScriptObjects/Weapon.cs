using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

[CreateAssetMenu(menuName = "Scriptable Objects/Weapon")]
public class Weapon : ScriptableObject, IInventoryItem
{
    public SpriteLibraryAsset spriteLibraryAsset;
    public AttackDataCombo combo;
    public Sprite icon;
    public string displayName;
    [TextArea] public string description;
    public List<AbilityData> passiveAbilities = new();

    public string DisplayName => displayName;
    public string Details => description;
    public Sprite Icon => icon;
}
