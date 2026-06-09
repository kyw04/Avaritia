using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BossAttackEntry
{
    [SerializeReference] public AttackData data;
    public float cooldown;
    public float maxRange = float.MaxValue;
}

[CreateAssetMenu(menuName = "Scriptable Objects/BossAttackData")]
public class BossAttackData : ScriptableObject
{
    public List<BossAttackEntry> entries = new();
}
