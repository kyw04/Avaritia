using UnityEngine;

public interface IInventoryItem : IInfo
{
    Entity Owner { get; }
}
