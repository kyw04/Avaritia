using System;

// 탄약 바 총알 수가 target에 새로 도달하는 순간 한 번 발동한다.
[Serializable]
public class OnAmmoReachedTrigger : IAbilityTrigger
{
    public AmmoTarget target;
    public int value;

    private Entity owner;
    private IAmmoModule weapon;
    private Action<AbilityContext> fire;

    public void Bind(Entity owner, AbilityData data, Action<AbilityContext> fire)
    {
        this.owner = owner;
        this.fire = fire;
        // 무기 어빌리티는 Player.EquipWeapon에서 weapon.Equip 이후에 바인딩되므로 이때의 Player.Weapon이 장착된 인스턴스다.
        if (owner is Player { Weapon: { } pw } && pw.TryGetModule<IAmmoModule>(out var w))
        {
            weapon = w;
            w.AmmoChanged += OnAmmoChanged;
        }
    }

    public void Unbind(Entity owner)
    {
        if (weapon == null) return;
        weapon.AmmoChanged -= OnAmmoChanged;
        weapon = null;
    }

    private void OnAmmoChanged(int prev, int current)
    {
        int t = target.Resolve(weapon, value);
        if (current == t && prev != t)
            fire(new AbilityContext { Caster = owner, Target = owner.transform });
    }
}
