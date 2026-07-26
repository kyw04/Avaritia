using UnityEngine;

public class WeaponPickup : IInteractable
{
    private readonly Weapon weapon;
    public WeaponPickup(Weapon weapon) => this.weapon = weapon;

    public string DisplayName => weapon.name;
    public Sprite Icon => weapon.icon;
    // Payload-only: never registered directly, always wrapped by WorldPickup.
    public Transform Transform => null;
    public bool NeedsChoice(Player player) => false;

    public void Interact(Player player, InteractChoice choice)
    {
        var previous = player.Weapon;
        player.EquipWeapon(weapon);
        if (previous != null)
            WorldInteractionManager.Instance.Spawn(new WeaponPickup(previous), player.transform.position);
    }
}
