using UnityEngine;

public enum PickupChoice { Primary, Secondary }

public interface IPickupable
{
    string DisplayName { get; }
    bool NeedsChoice(Player player);
    bool TapResolves(Player player);
    void Pickup(Player player, PickupChoice choice, Vector3 dropPosition);
}
