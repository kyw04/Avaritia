using System;
using System.Collections.Generic;

public class Inventory
{
    public const int ItemCapacity = 8;
    private readonly List<IInventoryItem> items = new();

    public IReadOnlyList<IInventoryItem> Items => items;
    public IInventoryItem SelectedItem { get; private set; }
    public event Action Changed;

    public bool TryAdd(IInventoryItem item)
    {
        if (items.Count >= ItemCapacity) return false;
        items.Add(item);
        Changed?.Invoke();
        return true;
    }

    public bool Remove(IInventoryItem item)
    {
        bool removed = items.Remove(item);
        if (removed) Changed?.Invoke();
        return removed;
    }

    public void Select(IInventoryItem item) => SelectedItem = item;
}
