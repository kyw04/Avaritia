// 탄창 무기로 지금 쏘는 총알을 강화한다. OnAttackStartTrigger로 발동해야 그 발사에 반영된다.
[System.Serializable]
public class EnhanceBulletEffect : IAbilityEffect
{
    public BulletEnhancement enhancement = new();

    public void Apply(AbilityContext context)
    {
        if (context.Caster is Player { Weapon: MagazineWeapon weapon })
            weapon.EnhanceCurrentBullet(enhancement);
    }
}
