using UnityEngine;

public class WeaponPickup : IInteractable
{
    private readonly WeaponInstance weapon;
    public WeaponPickup(WeaponInstance weapon) => this.weapon = weapon;
    public WeaponPickup(WeaponData data) : this(new WeaponInstance(data)) { }

    public WeaponInstance Weapon => weapon;
    public string DisplayName => weapon.DisplayName;
    public Sprite Icon => weapon.Icon;
    // Payload-only: never registered directly, always wrapped by WorldPickup.
    public Transform Transform => null;
    public bool NeedsChoice(Player player) => false;

    public void Interact(Player player, InteractChoice choice)
    {
        var previous = player.Weapon;
        player.EquipWeapon(weapon);
        EventBus.Publish(new PlayerEquippedEvent(player));
        if (previous != null)
            WorldInteractionManager.Instance.Spawn(new WeaponPickup(previous), player.transform.position);
    }
}
