using System;

[System.Serializable]
public class ManualTrigger : IAbilityTrigger
{
    public void Bind(Entity owner, AbilityData data, Action<AbilityContext> fire) { }
    public void Unbind(Entity owner) { }
}
