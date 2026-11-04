using System;

// 무기 기능을 조합하기 위한 모듈. WeaponData.modules에 담겨 장착/공격/점프 시점에 호출된다.
// 탄약 바를 쓰는 모듈은 무기당 하나만 둔다.
[Serializable]
public abstract class WeaponModule
{
    protected WeaponInstance Weapon { get; private set; }
    protected Entity Owner => Weapon.Owner;

    public void Bind(WeaponInstance weapon) => Weapon = weapon;

    // 데이터 에셋의 모듈은 설정 원본이므로 인스턴스마다 복사해 런타임 상태를 따로 갖게 한다.
    // 상태 컬렉션을 가진 모듈은 재정의해 새 컬렉션을 만든다.
    public virtual WeaponModule CreateRuntimeCopy() => (WeaponModule)MemberwiseClone();

    public virtual bool UsesCountBar => false;
    public virtual void Equip(ImageCountBar countBar) { }
    public virtual void Unequip() { }

    // false를 반환하면 이번 공격을 취소한다.
    public virtual bool BeforeAttack(WeaponAttack attack) => true;
    public virtual void AfterAttack(WeaponAttack attack) { }
    public virtual bool ConsumeJump() => false;
}
