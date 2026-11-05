using System;
using System.Collections.Generic;
using UnityEngine;

// 대장장이 모듈: 스탯 조각(ForgePickup)을 먹을 때마다 rewards 중 하나를 무작위로 골라 쌓는다.
// 쌓인 스탯은 이 무기를 들고 있는 동안만 버프로 적용되고, 버렸다 다시 주우면 쌓인 만큼 다시 적용된다.
// 조각 생성은 passiveAbilities의 OnKillTrigger + ChanceCondition + SpawnPrefabEffect가 맡는다.
[Serializable]
public class ForgeModule : WeaponModule
{
    [Serializable]
    public class Reward
    {
        public StatType statType;
        public BuffValueType valueType;
        public float amount;
        public Sprite icon;
    }

    [SerializeField] private List<Reward> rewards = new();

    // rewards와 같은 순서의 획득 횟수.
    private int[] counts = Array.Empty<int>();
    private bool equipped;

    public override WeaponModule CreateRuntimeCopy()
    {
        var copy = (ForgeModule)base.CreateRuntimeCopy();
        copy.counts = new int[rewards.Count];
        return copy;
    }

    public override void Equip(ImageCountBar countBar)
    {
        equipped = true;
        ApplyBuffs();
    }

    public override void Unequip()
    {
        equipped = false;
        Owner.RemoveBuffsBySource(this);
    }

    public void Collect()
    {
        if (rewards.Count == 0)
            return;

        counts[UnityEngine.Random.Range(0, rewards.Count)]++;
        if (equipped)
            ApplyBuffs();
    }

    // 같은 출처/스탯/값 종류의 버프는 덮어쓰이므로, 스탯이 겹치는 항목끼리 합쳐서 건다.
    private void ApplyBuffs()
    {
        var totals = new Dictionary<(StatType, BuffValueType), float>();
        for (int i = 0; i < rewards.Count; i++)
        {
            if (counts[i] == 0)
                continue;
            var key = (rewards[i].statType, rewards[i].valueType);
            totals.TryGetValue(key, out float total);
            totals[key] = total + rewards[i].amount * counts[i];
        }

        foreach (var pair in totals)
            Owner.ApplyBuff(this, pair.Key.Item1, pair.Key.Item2, pair.Value, 0f);
    }

    public override void CollectStatusIcons(List<StatusIconEntry> into)
    {
        for (int i = 0; i < rewards.Count; i++)
        {
            if (counts[i] == 0 || rewards[i].icon == null)
                continue;

            into.Add(new StatusIconEntry
            {
                Key = (rewards[i], StatusIconKind.Buff),
                Icon = rewards[i].icon,
                EndTime = float.MaxValue,
                Count = counts[i],
            });
        }
    }
}
