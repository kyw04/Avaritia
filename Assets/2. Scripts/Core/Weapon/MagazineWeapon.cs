using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Weapon/MagazineWeapon")]
public class MagazineWeapon : Weapon, IAmmoWeapon
{
    [SerializeField] private ImageCountPoolable ammoImagePrefab;
    [SerializeField] private int maxAmmo;
    [SerializeField] private float reloadTime;
    
    private ImageCountBar ammoCountBar;
    private int currentAmmo;
    private Coroutine coroutine;
    private BulletEnhancement pendingEnhancement;

    // 지금 쏘는 총알이 탄창의 몇 번째인지 (재장전 직후 첫 발 = 1)
    public int CurrentBulletNumber => maxAmmo - currentAmmo;

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;
    public event Action<int, int> AmmoChanged;

    private void SetAmmo(int value)
    {
        if (value == currentAmmo)
            return;

        int prev = currentAmmo;
        currentAmmo = value;
        ammoCountBar?.Refresh();
        AmmoChanged?.Invoke(prev, value);
    }

    public void EnhanceCurrentBullet(BulletEnhancement enhancement) => pendingEnhancement = enhancement;

    public void Reload()
    {
        if (coroutine != null)
            return;

        coroutine = Owner.Mono.StartCoroutine(Reloading());
    }

    private IEnumerator Reloading()
    {
        Debug.Log("Reloading...");
        yield return new WaitForSeconds(reloadTime);
        Debug.Log("Ready!");
        SetAmmo(maxAmmo);
        coroutine = null;
    }

    public override void Equip(Entity owner, ImageCountBar countBar = null)
    {
        base.Equip(owner, countBar);
        currentAmmo = maxAmmo;

        ammoCountBar = countBar;
        ammoCountBar?.Init(ammoImagePrefab, () => currentAmmo, () => maxAmmo, AmmoIconAt);
        ammoCountBar?.Activate();
    }

    public override void Unequip()
    {
        base.Unequip();
        ammoCountBar?.Deactivate();
        if (coroutine != null)
        {
            Owner.Mono.StopCoroutine(coroutine);
            coroutine = null;
        }
    }

    public override void Attack(IAttacker attacker, int index = 0, Transform defender = null)
    {
        if (currentAmmo <= 0)
        {
            Reload();
            return;
        }

        SetAmmo(currentAmmo - 1);

        // 공격 시작 이벤트 동안 AmmoCondition + EnhanceBulletEffect 어빌리티가 이번 총알을 강화할 수 있다.
        // 탄을 깎은 뒤 발행하므로 N번째 총알은 남은 탄 (maxAmmo - N)발일 때다 (마지막 총알 = 0발 = Min).
        pendingEnhancement = null;
        EventBus.Publish(new EntityAttackStartEvent(Owner, combo.datas[index]));
        combo.datas[index].Attack(attacker, defender, pendingEnhancement);
    }

    // 탄약 UI는 마지막 칸부터 사라지므로 i번째 칸의 총알을 쏜 직후 남은 탄은 i발이다.
    private Sprite AmmoIconAt(int index)
    {
        foreach (var ability in passiveAbilities)
        {
            if (ability == null || !ability.conditions.Exists(c =>
                    c is AmmoCondition a && a.comparison == AmmoComparison.Equal && a.target.Resolve(this, a.value) == index))
                continue;
            if (ability.effects.Find(e => e is EnhanceBulletEffect) is EnhanceBulletEffect effect)
                return effect.enhancement.ammoIcon;
        }
        return null;
    }

}