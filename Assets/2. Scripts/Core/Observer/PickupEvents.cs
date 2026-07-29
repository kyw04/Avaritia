public struct PickupCollectedEvent : ISubject
{
    public WorldPickup Pickup { get; private set; }
    public PickupCollectedEvent(WorldPickup pickup) { Pickup = pickup; }
}
