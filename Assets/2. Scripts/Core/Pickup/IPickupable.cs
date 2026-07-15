public enum PickupChoice { Primary, Secondary }

public interface IPickupable
{
    string DisplayName { get; }
    bool NeedsChoice(Player player);
    void Pickup(Player player, PickupChoice choice, UnityEngine.Vector3 dropPosition);
}
