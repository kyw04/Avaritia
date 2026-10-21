using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Weapon/MagazineWeapon")]
public class MagazineWeapon : Weapon
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
        currentAmmo = maxAmmo;
        ammoCountBar?.Refresh();
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

        currentAmmo--;
        ammoCountBar?.Refresh();

        // 공격 시작 이벤트 동안 MagazineBulletCondition + EnhanceBulletEffect 어빌리티가 이번 총알을 강화할 수 있다.
        pendingEnhancement = null;
        EventBus.Publish(new EntityAttackStartEvent(Owner, combo.datas[index]));
        combo.datas[index].Attack(attacker, defender, pendingEnhancement);
    }

    // 탄약 UI는 마지막 칸부터 사라지므로 i번째 칸은 (maxAmmo - i)번째로 발사될 총알이다.
    private Sprite AmmoIconAt(int index)
    {
        int bulletNumber = maxAmmo - index;
        foreach (var ability in passiveAbilities)
        {
            if (ability == null || !ability.conditions.Exists(c => c is MagazineBulletCondition b && b.bulletNumber == bulletNumber))
                continue;
            if (ability.effects.Find(e => e is EnhanceBulletEffect) is EnhanceBulletEffect effect)
                return effect.enhancement.ammoIcon;
        }
        return null;
    }

}