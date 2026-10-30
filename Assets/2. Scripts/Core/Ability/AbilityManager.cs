using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class AbilityManager
{
    // A single bound instance of an AbilityData. Multiple bindings can share the same `Data`
    // reference (e.g. two copies of the same Item held at once) — each still gets its own
    // trigger/state so they activate, go on cooldown, and stack buffs independently.
    private class Binding
    {
        public AbilityData Data;
        public IAbilityTrigger Trigger;
        public AbilityRuntimeState State;
    }

    private readonly Entity owner;
    private readonly AbilityData[] abilities;
    private readonly bool publishEvents;
    private readonly List<Binding> bindings = new();

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
        foreach (var binding in bindings)
        {
            binding.Trigger.Unbind(owner);
            owner.RemoveBuffsBySource(binding.State);
        }
        bindings.Clear();
    }

    // Incrementally reconciles the bound abilities to `newAbilities` (a multiset — duplicate
    // AbilityData references are meaningful, e.g. two copies of the same passive Item).
    // Bindings whose AbilityData still appears are kept as-is (same AbilityRuntimeState/trigger,
    // so their cooldown/buffs survive); ones no longer present are unbound and have any buffs
    // they applied removed; new entries get a fresh binding. This is what lets Player.ItemAbilities
    // rebuild on every Inventory change without losing per-copy state or leaking buffs.
    public void Rebind(AbilityData[] newAbilities)
    {
        var remaining = new List<Binding>(bindings);
        var next = new List<Binding>();

        foreach (var data in newAbilities)
        {
            var match = remaining.Find(b => b.Data == data);
            if (match != null)
            {
                remaining.Remove(match);
                next.Add(match);
            }
            else
            {
                var created = CreateBinding(data);
                if (created != null) next.Add(created);
            }
        }

        foreach (var gone in remaining)
        {
            gone.Trigger.Unbind(owner);
            owner.RemoveBuffsBySource(gone.State);
        }

        bindings.Clear();
        bindings.AddRange(next);
    }

    // Shared by BindAll (spawn/weapon-equip) and SetAbility (runtime equip) so a newly-equipped ability gets
    // the same AbilityRuntimeState/trigger binding a spawn-time one does — without this, TryActivate
    // finds no bound state for an ability equipped after spawn and silently refuses to activate it.
    private void BindOne(AbilityData data)
    {
        var binding = CreateBinding(data);
        if (binding != null) bindings.Add(binding);
    }

    private Binding CreateBinding(AbilityData data)
    {
        if (data == null || data.trigger == null) return null;
        var binding = new Binding { Data = data, State = new AbilityRuntimeState(), Trigger = CloneTrigger(data.trigger) };
        binding.Trigger.Bind(owner, data, ctx => TryActivate(binding, ctx));
        return binding;
    }

    private void UnbindOne(AbilityData data)
    {
        var binding = bindings.Find(b => b.Data == data);
        if (binding == null) return;
        binding.Trigger.Unbind(owner);
        owner.RemoveBuffsBySource(binding.State);
        bindings.Remove(binding);
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

    public bool IsOnCooldown(AbilityData data)
    {
        var binding = bindings.Find(b => b.Data == data);
        return binding != null && Time.time < binding.State.CooldownEndTime;
    }

    public bool TryActivate(AbilityData data, AbilityContext context)
    {
        var binding = bindings.Find(b => b.Data == data);
        return binding != null && TryActivate(binding, context);
    }

    private bool TryActivate(Binding binding, AbilityContext context)
    {
        var data = binding.Data;
        context.State = binding.State;
        if (data.conditions.Count > 0 && data.conditions.Exists(c => !c.IsMet(context))) return false;

        binding.State.ActivationCount++;
        data.cast.Play(owner.transform.position);
        if (data.cast.animClip != null)
            EventBus.Publish(new EntityAnimationEvent(owner, data.cast.animClip));
        foreach (var effect in data.effects)
            effect?.Apply(context);

        float cooldown = CooldownDuration(data);
        binding.State.CooldownEndTime = IsLocking(data) ? float.MaxValue : Time.time + cooldown;
        if (publishEvents)
            EventBus.Publish(new EntitySkillCooldownEvent(owner, Array.IndexOf(abilities, data), cooldown, binding.State.CooldownEndTime));
        return true;
    }

    // cooldown이 음수면 쿨타임이 돌지 않고 잠긴다. ReduceCooldown으로 풀어 줄 때까지 다시 쓸 수 없다(UI는 가득 찬 채로 보인다).
    private static bool IsLocking(AbilityData data) => data.cooldown < 0f;

    private float CooldownDuration(AbilityData data) => IsLocking(data)
        ? float.MaxValue
        : data.cooldown * (1f - owner.GetStat<float>(StatType.CooldownReduction) / 100f);

    // 남은 쿨타임을 seconds만큼 줄인다(남은 시간보다 크면 바로 쓸 수 있게 된다). 잠긴 쿨타임은 바로 풀린다.
    // 이 매니저에 바인딩된 state가 아니면 false.
    public bool ReduceCooldown(AbilityRuntimeState state, float seconds)
    {
        var binding = bindings.Find(b => b.State == state);
        if (binding == null) return false;

        bool locked = state.CooldownEndTime == float.MaxValue;
        state.CooldownEndTime = locked ? Time.time : Mathf.Max(Time.time, state.CooldownEndTime - seconds);
        // 다 줄었으면 Duration 0으로 보내 UI를 비우고, 남았으면 원래 길이 기준으로 채움 비율을 이어 간다.
        float duration = state.CooldownEndTime > Time.time ? CooldownDuration(binding.Data) : 0f;
        if (publishEvents)
            EventBus.Publish(new EntitySkillCooldownEvent(owner, Array.IndexOf(abilities, binding.Data), duration, state.CooldownEndTime));
        return true;
    }
}
