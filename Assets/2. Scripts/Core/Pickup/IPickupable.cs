using UnityEngine;

public enum PickupChoice { Primary, Secondary }

public interface IPickupable
{
    string DisplayName { get; }
    Sprite Icon { get; }
    bool NeedsChoice(Player player);
    void Pickup(Player player, PickupChoice choice, Vector3 dropPosition);
}
