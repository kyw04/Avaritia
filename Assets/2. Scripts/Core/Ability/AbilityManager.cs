using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class AbilityManager
{
    private readonly Entity owner;
    private readonly AbilityData[] abilities;
    private readonly bool publishEvents;
    private readonly Dictionary<AbilityData, AbilityRuntimeState> states = new();
    private readonly Dictionary<AbilityData, IAbilityTrigger> boundTriggers = new();

    public AbilityManager(Entity owner, AbilityData[] abilities, bool publishEvents = true)
    {
        this.owner = owner;
        this.abilities = abilities ?? Array.Empty<AbilityData>();
        this.publishEvents = publishEvents;
    }

    public void BindAll()
    {
        foreach (var a in abilities)
            BindOne(a);
    }

    public void UnbindAll()
    {
        foreach (var trigger in boundTriggers.Values)
            trigger.Unbind(owner);
        boundTriggers.Clear();
        states.Clear();
    }

    // Shared by BindAll (spawn/weapon-equip) and SetAbility (runtime equip) so a newly-equipped ability gets
    // the same AbilityRuntimeState/trigger binding a spawn-time one does — without this, TryActivate
    // finds no `states` entry for an ability equipped after spawn and silently refuses to activate it.
    private void BindOne(AbilityData data)
    {
        if (data == null || data.trigger == null) return;
        states[data] = new AbilityRuntimeState();
        var trigger = CloneTrigger(data.trigger);
        boundTriggers[data] = trigger;
        trigger.Bind(owner, ctx => TryActivate(data, ctx));
    }

    private void UnbindOne(AbilityData data)
    {
        if (data == null || !boundTriggers.TryGetValue(data, out var trigger)) return;
        trigger.Unbind(owner);
        boundTriggers.Remove(data);
        states.Remove(data);
    }

    // AbilityData is a shared ScriptableObject asset multiple Entities can reference (e.g. several
    // enemies of the same type equipping the same ability). Its `trigger` is [SerializeReference]
    // data living on that shared asset, so binding directly against it would let one Entity's Bind()
    // overwrite another's owner/callback/coroutine state. Cloning gives each Entity its own trigger
    // instance to hold that per-binding state — the same reason AbilityRuntimeState (cooldown) is
    // kept outside AbilityData rather than as a field on it.
    private static readonly MethodInfo MemberwiseCloneMethod =
        typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

    private static IAbilityTrigger CloneTrigger(IAbilityTrigger source) =>
        (IAbilityTrigger)MemberwiseCloneMethod.Invoke(source, null);

    public List<AbilityData> GetAvailableAbilities(float targetDistance)
    {
        var available = new List<AbilityData>();
        foreach (var data in abilities)
        {
            if (data == null) continue;
            if (targetDistance > data.maxRange) continue;
            if (IsOnCooldown(data)) continue;
            available.Add(data);
        }
        return available;
    }

    public AbilityData AbilityAt(int index) =>
        index >= 0 && index < abilities.Length ? abilities[index] : null;

    public AbilityData SetAbility(int index, AbilityData data)
    {
        if (index < 0 || index >= abilities.Length) return null;
        var previous = abilities[index];
        abilities[index] = data;

        UnbindOne(previous);
        BindOne(data);

        if (publishEvents)
            EventBus.Publish(new EntitySkillEquippedEvent(owner, index, data));
        return previous;
    }

    public bool IsOnCooldown(AbilityData data) =>
        data != null && states.TryGetValue(data, out var s) && Time.time < s.CooldownEndTime;

    public bool TryActivate(AbilityData data, AbilityContext context)
    {
        if (data == null || !states.TryGetValue(data, out var state)) return false;
        context.State = state;
        if (data.conditions.Exists(c => !c.IsMet(context))) return false;

        foreach (var effect in data.effects)
            effect?.Apply(context);

        state.CooldownEndTime = Time.time + data.cooldown;
        if (publishEvents)
            EventBus.Publish(new EntitySkillCooldownEvent(owner, Array.IndexOf(abilities, data), data.cooldown, state.CooldownEndTime));
        return true;
    }
}
