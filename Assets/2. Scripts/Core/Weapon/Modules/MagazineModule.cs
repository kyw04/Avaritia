using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class MagazineModule : WeaponModule, IAmmoModule
{
    [SerializeField] private ImageCountStyle ammoStyle;
    [SerializeField] private int maxAmmo;
    [SerializeField] private float reloadTime;
    // 탄이 닳은 채로 이 시간 동안 쏘지 않으면 장전을 시작한다. 장전 중에 쏘면 장전이 끊긴다.
    [SerializeField] private float idleReloadDelay = 1f;

    private ImageCountBar ammoCountBar;
    private int currentAmmo;
    private Coroutine coroutine;
    private bool reloading;
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
        if (reloading)
            return;

        StartReload(0f);
    }

    private void StartReload(float delay)
    {
        StopReload();
        coroutine = Owner.Mono.StartCoroutine(Reloading(delay));
    }

    private void StopReload()
    {
        if (coroutine != null)
        {
            Owner.Mono.StopCoroutine(coroutine);
            coroutine = null;
        }
        reloading = false;
    }

    private IEnumerator Reloading(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        reloading = true;
        Debug.Log("Reloading...");
        yield return new WaitForSeconds(reloadTime);
        Debug.Log("Ready!");
        SetAmmo(maxAmmo);
        reloading = false;
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
        ammoCountBar?.Init(ammoStyle, () => currentAmmo, () => maxAmmo, AmmoLookAt);
        ammoCountBar?.Activate();

        if (currentAmmo < maxAmmo)
            StartReload(idleReloadDelay);
    }

    public override void Unequip()
    {
        ammoCountBar?.Deactivate();
        StopReload();
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
        // 쏘면 진행 중이던 장전(또는 대기)을 끊고 대기 시간을 처음부터 다시 잰다.
        StartReload(idleReloadDelay);
        return true;
    }

    // 탄약 UI는 마지막 칸부터 사라지므로 i번째 칸의 총알을 쏜 직후 남은 탄은 i발이다.
    private GaugeSlotLook AmmoLookAt(int index)
    {
        foreach (var ability in Weapon.Data.passiveAbilities)
        {
            if (ability == null || !ability.conditions.Exists(c =>
                    c is AmmoCondition a && a.comparison == AmmoComparison.Equal && a.target.Resolve(this, a.value) == index))
                continue;
            if (ability.effects.Find(e => e is EnhanceBulletEffect) is EnhanceBulletEffect effect)
                return effect.enhancement.ammoLook;
        }
        return null;
    }
}
