public struct PickupCollectedEvent : ISubject
{
    public WorldPickup Pickup { get; private set; }
    public PickupCollectedEvent(WorldPickup pickup) { Pickup = pickup; }
}

public struct PickupSpawnedEvent : ISubject
{
    public WorldPickup Pickup { get; private set; }
    public PickupSpawnedEvent(WorldPickup pickup) { Pickup = pickup; }
}

public struct ItemBoxSpawnedEvent : ISubject
{
    public ItemBox Box { get; private set; }
    public ItemBoxSpawnedEvent(ItemBox box) { Box = box; }
}
