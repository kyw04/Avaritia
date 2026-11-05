using System;
using UnityEngine;

// 피해의 종류와, 피해에 반응하는 쪽이 무시할 것. 반응하는 쪽은 종류(DamageOverTime 등)가 아니라
// 동작 플래그(NoKnockback 등)만 본다. 그래야 새 피해 종류가 생겨도 반응하는 쪽 코드는 그대로다.
// 직렬화 값이므로 새 항목은 맨 뒤에만 추가할 것.
[Flags]
public enum DamageFlags
{
    None = 0,
    DamageOverTime = 1,  // 종류 표시(지속 피해)
    NoKnockback = 2,     // 무기 넉백을 걸지 않는다
    NoHitReaction = 4,   // 피격 연출(번쩍임, 화면 흔들림, 피격 소리/파티클)을 하지 않는다
    NoInterrupt = 8,     // 채널링 피격 중단, 피격 시 사라지는 버프를 건드리지 않는다
    NoOnHitEffects = 16, // 적중 트리거와 무기의 적중 효과(스택, 연쇄 번개 등)를 일으키지 않는다
    IgnoreArmor = 32,
    IgnoreEvasion = 64,
}

public struct DamageInfo
{
    public float Amount;
    // 준 쪽. 모르면 null(효과 등). 처치 판정(LastAttacker)에 쓴다.
    public Entity Attacker;
    // 실제로 맞은 위치(탄환 등). 모르면 null.
    public Vector3? HitPoint;
    // 연출용(데미지 팝업 색 등).
    public bool IsCritical;
    public DamageFlags Flags;

    public DamageInfo(float amount, Entity attacker = null, Vector3? hitPoint = null, bool isCritical = false,
        DamageFlags flags = DamageFlags.None)
    {
        Amount = amount;
        Attacker = attacker;
        HitPoint = hitPoint;
        IsCritical = isCritical;
        Flags = flags;
    }

    public bool Has(DamageFlags flag) => (Flags & flag) != 0;
}
