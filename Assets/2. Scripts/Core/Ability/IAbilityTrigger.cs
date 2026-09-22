using System;

public interface IAbilityTrigger
{
    void Bind(Entity owner, Action<AbilityContext> fire);
    void Unbind(Entity owner);
}
