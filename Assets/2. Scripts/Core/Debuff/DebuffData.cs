using System;
using System.Collections.Generic;
using UnityEngine;

// 직렬화 값이므로 새 항목은 맨 뒤에만 추가할 것.
[Flags] public enum DebuffTag { None = 0, Control = 1, Weaken = 2, DamageOverTime = 4 }

// Refresh: 남은 시간을 새 값과 기존 값 중 긴 쪽으로. Extend: 남은 시간에 더함.
// Stack: 중첩 수 +1(maxStacks까지), 시간은 Refresh 규칙. 새 항목은 맨 뒤에만 추가할 것.
public enum DebuffStackRule { Refresh, Extend, Stack }

// 디버프 설정. 실제 효과는 modules 조합으로 정하고, 런타임 상태는 ActiveDebuff가 갖는다.
// 대상에게 거는 입구는 Entity.Debuffs.Apply 하나다.
[CreateAssetMenu(menuName = "Scriptable Objects/Debuff")]
public class DebuffData : ScriptableObject
{
    public string displayName;
    // 비우면 상태 아이콘을 표시하지 않는다.
    public Sprite icon;
    public DebuffTag tags;
    public DebuffStackRule stackRule;
    [Min(1)] public int maxStacks = 1;
    // 0 = 제한 없음. 건 쪽의 DebuffDuration 보너스까지 더한 뒤 이 값으로 자른다.
    [Min(0)] public float maxDuration;
    [SerializeReference, SubclassSelector] public List<DebuffModule> modules = new();
}
