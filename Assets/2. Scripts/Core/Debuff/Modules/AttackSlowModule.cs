using System;
using UnityEngine;

// 디버프가 걸려 있는 동안 공격 준비/진행 시간과 공격 애니메이션을 느리게 한다.
// 여러 개가 걸려 있으면 가장 강한(작은) 배율만 쓴다(Entity.AttackSpeedMultiplier).
[Serializable]
public class AttackSlowModule : DebuffModule
{
    [Range(0.05f, 1f)] public float multiplier = 0.5f;
}
