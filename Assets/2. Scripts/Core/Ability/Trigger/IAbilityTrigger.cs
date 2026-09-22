using System;

public interface IAbilityTrigger
{
    // `data` is the specific AbilityData this trigger instance is bound to — triggers that listen
    // for a broadcast event about "some ability" (e.g. OnEquippedTrigger) need it to ignore events
    // meant for a different AbilityData/Item.
    void Bind(Entity owner, AbilityData data, Action<AbilityContext> fire);
    void Unbind(Entity owner);
}
