using System;

// 무기 기능을 조합하기 위한 모듈. Weapon.modules에 담겨 장착/공격/점프 시점에 호출된다.
// 탄약 바를 쓰는 모듈은 무기당 하나만 둔다.
[Serializable]
public abstract class WeaponModule
{
    protected Weapon Weapon { get; private set; }
    protected Entity Owner => Weapon.Owner;

    public void Bind(Weapon weapon) => Weapon = weapon;

    public virtual bool UsesCountBar => false;
    public virtual void Equip(ImageCountBar countBar) { }
    public virtual void Unequip() { }

    // false를 반환하면 이번 공격을 취소한다.
    public virtual bool BeforeAttack(WeaponAttack attack) => true;
    public virtual void AfterAttack(WeaponAttack attack) { }
    public virtual bool ConsumeJump() => false;
}
