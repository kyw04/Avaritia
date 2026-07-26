using UnityEngine;

public class WeaponPickup : IInteractable
{
    private readonly Weapon weapon;
    public WeaponPickup(Weapon weapon) => this.weapon = weapon;

    public string DisplayName => weapon.name;
    public Sprite Icon => weapon.icon;
    public bool NeedsChoice(Player player) => false;

    public void Interact(Player player, InteractChoice choice, Vector3 dropPosition)
    {
        var previous = player.Weapon;
        player.EquipWeapon(weapon);
        if (previous != null)
            WorldPickupManager.Instance.Spawn(new WeaponPickup(previous), dropPosition);
    }
}
