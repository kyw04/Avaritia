using System;
using UnityEngine;

// 포탑 모듈: 포탑(제자리에 서서 총을 쏘는 아군)을 하나만 설치하고, 가까이에서 수리한다.
// 다시 설치하면 기존 포탑을 거두고 새로 세운다. 쿨타임은 InputCooldownModule이 맡으며,
// 수리는 실제로 수리했을 때만 쿨타임을 시작한다(수리 바인딩의 쿨타임 모듈은 startOnInput을 끈다).
[Serializable]
public class TurretModule : WeaponModule
{
    [SerializeField] private Enemy turretPrefab;
    [SerializeField] private Vector2 spawnOffset;
    [SerializeField, Min(0)] private float repairRange = 2f;
    // 수리 한 번에 회복하는 양 (포탑 최대 체력의 %)
    [SerializeField, Range(0f, 100f)] private float repairPercent = 30f;

    // 무기 인스턴스에 남으므로 버렸다 다시 주워도 같은 포탑을 관리한다.
    private Enemy turret;
    private const string RepairMethod = nameof(TurretModule) + "." + nameof(Repair);

    // 수리한 아래 공격 입력을 떼기 전까지 같은 키로 나가는 공격을 막는다.
    private bool blockAttack;

    public override void Unequip() => blockAttack = false;

    public override bool BeforeAttack(WeaponAttack attack) => !blockAttack;

    [InputMethod]
    private void Build()
    {
        if (IsAlive(turret))
            ObjectPoolManager.Instance.Despawn(turret.gameObject);

        var position = Owner.transform.position + (Vector3)spawnOffset;
        turret = ObjectPoolManager.Instance.Spawn(turretPrefab, position, Quaternion.identity);
        turret.ConvertToAlly(Owner.transform, 1f);
    }

    [InputMethod]
    private void Repair()
    {
        if (!IsAlive(turret) || Vector2.Distance(Owner.transform.position, turret.transform.position) > repairRange)
            return;

        turret.Heal(turret.MaxHealth * repairPercent / 100f);
        Weapon.StartInputCooldown(RepairMethod);
        blockAttack = true;
    }

    public override void OnReleased(WeaponData.InputMethodBinding binding)
    {
        if (binding.methodName == RepairMethod)
            blockAttack = false;
    }
}
