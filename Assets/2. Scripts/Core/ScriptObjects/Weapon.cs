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
    public PooledParticle attackParticle;
    public float shakeForce = 0.3f;
    [Range(0f, 1f)] public float hitStopTimeScale = 0.1f;
    [Min(0)] public float hitStopDuration = 0.08f;

    public string DisplayName => displayName;
    public string Details => description;
    public Sprite Icon => icon;
}
