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
        ammoCountBar?.Init(ammoImagePrefab, () => currentAmmo, () => maxAmmo);
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
        base.Attack(attacker, index, defender);
    }
    
}