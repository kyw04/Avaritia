using UnityEngine;

public interface IInventoryItem
{
    Entity Owner { get; }
    string DisplayName { get; }
    string Details { get; }
    Sprite Icon { get; }
}
