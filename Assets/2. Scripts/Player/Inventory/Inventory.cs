using System.Collections.Generic;

public class Inventory
{
    private readonly List<IInventoryItem> items = new();

    public IReadOnlyList<IInventoryItem> Items => items;
    public void Add(IInventoryItem item) => items.Add(item);
    public bool Remove(IInventoryItem item) => items.Remove(item);
}
