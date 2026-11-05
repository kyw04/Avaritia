using System.Collections.Generic;
using UnityEngine;

// 대상 하나에 걸린 디버프 목록. 걸기/갱신/만료/해제를 모두 여기서 처리한다.
// 끝날 때는 경로와 상관없이 End 하나를 거쳐, 모듈이 ActiveDebuff를 출처로 건 버프를 한 번에 걷어낸다.
public class DebuffController
{
    private readonly Entity owner;
    private readonly List<ActiveDebuff> active = new();

    public DebuffController(Entity owner) => this.owner = owner;

    // 안 걸리면(데이터 없음, 시간 0 이하, 대상 사망, 모든 모듈 면역) null.
    // 같은 디버프가 이미 있으면 누가 걸었든 그 인스턴스를 갱신하고 건 사람만 바꾼다.
    public ActiveDebuff Apply(DebuffData data, float duration, Entity inflictor)
    {
        if (data == null || duration <= 0f || owner.IsDead)
            return null;

        float final = duration + (inflictor != null ? inflictor.DebuffDurationBonus : 0f);
        if (data.maxDuration > 0f)
            final = Mathf.Min(final, data.maxDuration);
        float now = Time.time;

        var debuff = Get(data);
        bool isNew = debuff == null;
        if (isNew)
        {
            var modules = data.modules.FindAll(m => m != null && m.Affects(owner));
            if (modules.Count == 0)
                return null;

            debuff = new ActiveDebuff(data, owner, modules) { Inflictor = inflictor, EndTime = now + final };
            debuff.Duration = final;
            active.Add(debuff);
            foreach (var module in debuff.Modules)
            {
                module.OnApply(debuff);
                // 걸리는 순간의 피해(tickOnApply)로 대상이 죽으면 이미 정리됐으므로 남은 모듈을 걸지 않는다.
                if (!active.Contains(debuff))
                    return null;
            }
        }
        else
        {
            debuff.Inflictor = inflictor;
            int previousStacks = debuff.Stacks;
            switch (data.stackRule)
            {
                case DebuffStackRule.Extend:
                    debuff.EndTime += final;
                    if (data.maxDuration > 0f)
                        debuff.EndTime = Mathf.Min(debuff.EndTime, now + data.maxDuration);
                    break;
                case DebuffStackRule.Stack:
                    debuff.Stacks = Mathf.Min(debuff.Stacks + 1, Mathf.Max(1, data.maxStacks));
                    debuff.EndTime = Mathf.Max(debuff.EndTime, now + final);
                    break;
                default:
                    debuff.EndTime = Mathf.Max(debuff.EndTime, now + final);
                    break;
            }
            debuff.Duration = debuff.EndTime - now;

            foreach (var module in debuff.Modules)
                module.OnReapply(debuff);
            if (debuff.Stacks != previousStacks)
                foreach (var module in debuff.Modules)
                    module.OnStacksChanged(debuff);
        }

        EventBus.Publish(new DebuffAppliedEvent(owner, inflictor, debuff, isNew));
        return debuff;
    }

    public void Remove(DebuffData data)
    {
        var debuff = Get(data);
        if (debuff != null)
            End(debuff);
    }

    public void RemoveByTag(DebuffTag tag)
    {
        foreach (var debuff in active.ToArray())
            if ((debuff.Data.tags & tag) != 0)
                End(debuff);
    }

    public void RemoveByInflictor(Entity inflictor)
    {
        foreach (var debuff in active.ToArray())
            if (debuff.Inflictor == inflictor)
                End(debuff);
    }

    public void ClearAll()
    {
        foreach (var debuff in active.ToArray())
            End(debuff);
    }

    public bool Has(DebuffData data) => Get(data) != null;

    public bool HasTag(DebuffTag tag) => active.Exists(d => (d.Data.tags & tag) != 0);

    public ActiveDebuff Get(DebuffData data) => active.Find(d => d.Data == data);

    // 걸려 있는 AttackSlowModule 중 가장 강한 배율. 없으면 1.
    public float AttackSpeedMultiplier
    {
        get
        {
            float multiplier = 1f;
            foreach (var debuff in active)
                foreach (var module in debuff.Modules)
                    if (module is AttackSlowModule slow)
                        multiplier = Mathf.Min(multiplier, slow.multiplier);
            return multiplier;
        }
    }

    public void Tick()
    {
        if (active.Count == 0)
            return;

        float now = Time.time;
        foreach (var debuff in active.ToArray())
        {
            // 앞선 디버프의 모듈이 이 디버프를 이미 끝냈을 수 있다.
            if (!active.Contains(debuff))
                continue;
            // 만료되는 프레임에도 먼저 틱을 돌려, 끝나는 시각에 걸친 마지막 틱(지속 피해 등)을 놓치지 않는다.
            foreach (var module in debuff.Modules)
            {
                module.OnTick(debuff);
                // 지속 피해로 대상이 죽으면 디버프가 모두 정리되므로 남은 모듈은 건너뛴다.
                if (!active.Contains(debuff))
                    break;
            }
            if (debuff.EndTime <= now)
                End(debuff);
        }
    }

    public void CollectStatusIcons(List<StatusIconEntry> into)
    {
        foreach (var debuff in active)
        {
            if (debuff.Data.icon == null)
                continue;

            into.Add(new StatusIconEntry
            {
                Key = (debuff.Data, StatusIconKind.Debuff),
                Icon = debuff.Data.icon,
                EndTime = debuff.EndTime,
                Duration = debuff.Duration,
                Count = debuff.Stacks > 1 ? debuff.Stacks : 0,
            });
        }
    }

    // 목록에서 먼저 빼서, 모듈이 끝나는 중에 다른 디버프를 걸거나 해제해도 꼬이지 않게 한다.
    private void End(ActiveDebuff debuff)
    {
        if (!active.Remove(debuff))
            return;

        foreach (var module in debuff.Modules)
            module.OnRemove(debuff);
        owner.RemoveBuffsBySource(debuff);
        owner.Restrictions.Remove(debuff);
        EventBus.Publish(new DebuffRemovedEvent(owner, debuff));
    }
}
