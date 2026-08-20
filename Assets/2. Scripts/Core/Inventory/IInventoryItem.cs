using UnityEngine;

public interface IInventoryItem
{
    string DisplayName { get; }
    string Details { get; }
    Sprite Icon { get; }
}
