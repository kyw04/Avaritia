using System;
using System.Collections.Generic;
using UnityEngine;

public class SkillManager
{
    private readonly SkillData[] skills;
    private readonly Dictionary<SkillData, float> cooldownEndTimes = new();

    public SkillManager(SkillData[] skills)
    {
        this.skills = skills ?? Array.Empty<SkillData>();
    }

    public List<SkillData> GetAvailableSkills(float targetDistance)
    {
        var available = new List<SkillData>();
        foreach (var data in skills)
        {
            if (data == null) continue;
            if (targetDistance > data.maxRange) continue;
            if (IsOnCooldown(data)) continue;
            available.Add(data);
        }
        return available;
    }

    public SkillData SkillAt(int index) =>
        index >= 0 && index < skills.Length ? skills[index] : null;

    public bool IsOnCooldown(SkillData data) =>
        data != null && cooldownEndTimes.TryGetValue(data, out var endTime) && Time.time < endTime;

    public bool TryUseSkill(SkillData data, IAttacker caster, Transform target = null)
    {
        if (data == null || IsOnCooldown(data)) return false;

        cooldownEndTimes[data] = Time.time + data.cooldown;
        data.Activate(caster, target);
        return true;
    }
}
