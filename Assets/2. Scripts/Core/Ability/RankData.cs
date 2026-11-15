using UnityEngine;

public enum AbilityRank
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
}

[CreateAssetMenu(fileName = "RankData", menuName = "Scriptable Objects/RankData")]
public class RankData : ScriptableObject
{
    public AbilityRank rank;
    public Color color = Color.white;
    [Range(0, 100)] public float dropPercent;
}