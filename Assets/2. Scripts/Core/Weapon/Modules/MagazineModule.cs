using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class MagazineModule : WeaponModule, IAmmoModule
{
    [SerializeField] private ImageCountPoolable ammoImagePrefab;
    [SerializeField] private int maxAmmo;
    [SerializeField] private float reloadTime;

    private ImageCountBar ammoCountBar;
    private int currentAmmo;
    private Coroutine coroutine;
    private bool initialized;

    // 지금 쏘는 총알이 탄창의 몇 번째인지 (재장전 직후 첫 발 = 1)
    public int CurrentBulletNumber => maxAmmo - currentAmmo;

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;
    public event Action<int, int> AmmoChanged;
    public bool GainPaused { get; set; }

    public override bool UsesCountBar => true;

    private void SetAmmo(int value)
    {
        if (value == currentAmmo)
            return;

        int prev = currentAmmo;
        currentAmmo = value;
        ammoCountBar?.Refresh();
        AmmoChanged?.Invoke(prev, value);
    }

    public void Consume() => SetAmmo(0);

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

    public override void Equip(ImageCountBar countBar)
    {
        // 처음 장착할 때만 채운다. 버렸다 다시 주운 무기는 남은 탄을 유지한다.
        if (!initialized)
        {
            currentAmmo = maxAmmo;
            initialized = true;
        }

        ammoCountBar = countBar;
        ammoCountBar?.Init(ammoImagePrefab, () => currentAmmo, () => maxAmmo, AmmoIconAt);
        ammoCountBar?.Activate();
    }

    public override void Unequip()
    {
        ammoCountBar?.Deactivate();
        if (coroutine != null)
        {
            Owner.Mono.StopCoroutine(coroutine);
            coroutine = null;
        }
    }

    // 탄을 깎은 뒤 공격 시작 이벤트가 발행되므로 N번째 총알은 남은 탄 (maxAmmo - N)발일 때다 (마지막 총알 = 0발 = Min).
    public override bool BeforeAttack(WeaponAttack attack)
    {
        if (currentAmmo <= 0)
        {
            Reload();
            return false;
        }

        SetAmmo(currentAmmo - 1);
        return true;
    }

    // 탄약 UI는 마지막 칸부터 사라지므로 i번째 칸의 총알을 쏜 직후 남은 탄은 i발이다.
    private Sprite AmmoIconAt(int index)
    {
        foreach (var ability in Weapon.Data.passiveAbilities)
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
