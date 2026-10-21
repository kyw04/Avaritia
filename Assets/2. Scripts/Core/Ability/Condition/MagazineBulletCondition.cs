using UnityEngine;

// 탄창 무기로 지금 쏘는 총알이 탄창의 bulletNumber번째일 때만 참 (재장전 직후 첫 발 = 1, 마지막 발 = maxAmmo).
// OnAttackStartTrigger + EnhanceBulletEffect와 함께 무기의 passiveAbilities에 넣어 그 총알을 강화하는 용도.
[System.Serializable]
public class MagazineBulletCondition : IAbilityCondition
{
    [Min(1)] public int bulletNumber = 1;

    public bool IsMet(AbilityContext context) =>
        context.Caster is Player { Weapon: MagazineWeapon weapon } && weapon.CurrentBulletNumber == bulletNumber;
}
