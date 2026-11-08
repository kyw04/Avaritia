using System;
using System.Collections.Generic;
using UnityEngine;

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

    // 입력 바인딩의 메서드를 실행하기 전에 묻는다. 하나라도 false면 실행하지 않는다(쿨타임 등).
    public virtual bool CanExecute(WeaponData.InputMethodBinding binding) => true;
    public virtual void OnExecuted(WeaponData.InputMethodBinding binding) { }
    // 바인딩(뗌 바인딩 제외)의 키를 뗐을 때 불린다. 누르는 동안만 유지하는 상태를 풀 때 쓴다.
    public virtual void OnReleased(WeaponData.InputMethodBinding binding) { }

    // 장착 중일 때 상태 아이콘 UI에 띄울 아이콘을 추가한다.
    public virtual void CollectStatusIcons(List<StatusIconEntry> into) { }

    // 다음 물리 프레임까지 넉백 상태로 두어 이동(상태 머신, 적 AI)이 이 속도를 덮어쓰지 못하게 한다.
    protected static void Hold(Entity entity, Vector2 velocity)
    {
        entity.ApplyKnockback(Vector2.zero, Time.fixedDeltaTime * 2f);
        entity.Rb.linearVelocity = velocity;
    }

    // 주인의 공격력과 치명타를 적용해 배율만큼 피해를 준다.
    protected void DealDamage(Entity target, float multiplier)
    {
        bool isCritical = UnityEngine.Random.Range(0f, 100f) < Owner.GetStat<float>(StatType.CritRate);
        float damage = Owner.GetDamageAgainst(target.transform) * multiplier * (isCritical ? CritConfig.Multiplier : 1f);
        target.TakeDamage(damage, Owner, target.transform.position, isCritical);
    }

    protected static bool IsAlive(Entity entity) => entity != null && entity.isActiveAndEnabled && entity.CurrentHealth > 0;
}
