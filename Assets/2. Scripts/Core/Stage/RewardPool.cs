using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct RewardPool
{
    public List<ScriptableObject> rewards; // Weapon, AbilityData(스킬), Item 에셋을 섞어서 넣을 수 있다.
    public int itemCount; // 상자 하나에서 나올 보상 개수
}
