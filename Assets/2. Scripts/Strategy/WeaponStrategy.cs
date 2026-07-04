public class WeaponStrategy
{
    private IWeapon weapon;

    public void SetWeapon(IWeapon weapon)
    {
        this.weapon = weapon;
    }

    public void AttackStrategy()
    {
        weapon.Offensive();
    }
}
