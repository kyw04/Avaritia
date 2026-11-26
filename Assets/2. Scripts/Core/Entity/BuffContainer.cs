using System.Collections.Generic;
using UnityEngine;

// 엔티티 하나의 스탯 버프 목록. 추가·갱신·제거와 스탯 계산(고정값 합 → 퍼센트 합)을 맡는다.
// 최대 체력 비율 유지처럼 엔티티 상태가 얽힌 처리는 Entity가 감싸서 부른다.
public class BuffContainer
{
    private class ActiveBuff
    {
        public object source;
        public StatType type;
        public BuffValueType valueType;
        public float amount;
        public float expireTime;
        public float duration; // 0 = 만료 없음. 상태 아이콘의 채움 비율용.
        public List<IAbilityCondition> conditions;
        public bool clearOnDamageTaken;
    }

    private readonly Entity owner;
    private readonly List<ActiveBuff> activeBuffs = new();
    // activeBuffs를 스탯별로 나눈 색인. 스탯 계산은 이 스탯의 버프만 본다(전체를 돌며 type을 비교하지 않는다).
    // 버프를 넣고 빼는 것은 Add/Remove/Clear로만 해서 두 목록을 맞춘다.
    private readonly Dictionary<StatType, List<ActiveBuff>> buffsByType = new();
    private static readonly List<ActiveBuff> NoBuffs = new();

    public BuffContainer(Entity owner) => this.owner = owner;

    public bool HasActive(StatType type)
    {
        foreach (var b in BuffsOf(type))
            if (IsActive(b, null))
                return true;
        return false;
    }

    public bool HasFrom(object source)
    {
        for (int i = 0; i < activeBuffs.Count; i++)
            if (activeBuffs[i].source == source)
                return true;
        return false;
    }

    public float GetAmount(object source, StatType type, BuffValueType valueType)
    {
        var existing = BuffsOf(type).Find(b => b.source == source && b.valueType == valueType && b.expireTime > Time.time);
        return existing?.amount ?? 0f;
    }

    public T Apply<T>(StatType type, T value, Transform target = null)
    {
        if (typeof(T) != typeof(float) && typeof(T) != typeof(int)) return value;

        if (typeof(T) == typeof(int))
        {
            // Int stats (DoubleJumpCount, DashCount, ...) only make sense as flat additions —
            // a "Percent" modifier is ignored here rather than silently truncating.
            int intResult = (int)(object)value;
            foreach (var b in BuffsOf(type))
            {
                if (b.valueType != BuffValueType.Flat || !IsActive(b, target)) continue;
                intResult += (int)b.amount;
            }
            return (T)(object)intResult;
        }

        float result = (float)(object)value;
        float flatSum = 0f, percentSum = 0f;
        foreach (var b in BuffsOf(type))
        {
            if (!IsActive(b, target)) continue;
            if (b.valueType == BuffValueType.Flat) flatSum += b.amount;
            else percentSum += b.amount;
        }

        result = (result + flatSum) * (1f + percentSum / 100f);
        return (T)(object)result;
    }

    public void AddOrUpdate(object source, StatType type, BuffValueType valueType, float amount, float expireTime,
        float duration, List<IAbilityCondition> conditions, bool clearOnDamageTaken)
    {
        var existing = BuffsOf(type).Find(b => b.source == source && b.valueType == valueType);
        if (existing != null)
        {
            existing.amount = amount;
            existing.expireTime = expireTime;
            existing.duration = Mathf.Max(0f, duration);
            existing.conditions = conditions;
            existing.clearOnDamageTaken = clearOnDamageTaken;
        }
        else
        {
            Add(new ActiveBuff
            {
                source = source, type = type, valueType = valueType, amount = amount, expireTime = expireTime,
                duration = Mathf.Max(0f, duration), conditions = conditions, clearOnDamageTaken = clearOnDamageTaken
            });
        }
    }

    public void RemoveExpired() => Remove(b => b.expireTime <= Time.time);

    public void RemoveClearedOnDamage() => Remove(b => b.clearOnDamageTaken);

    public void RemoveBySource(object source) => Remove(b => b.source == source);

    public void Clear()
    {
        activeBuffs.Clear();
        foreach (var list in buffsByType.Values)
            list.Clear();
    }

    // 상태 아이콘 UI용. 아이콘을 알 수 없는 출처(AbilityRuntimeState가 아닌 source 등)는 건너뛴다.
    public void CollectStatusIcons(List<StatusIconEntry> into)
    {
        foreach (var b in activeBuffs)
        {
            if (b.source is not AbilityRuntimeState state || state.Icon == null || !IsActive(b, null)) continue;

            var key = (b.source, StatusIconKind.Buff);
            int index = into.FindIndex(e => e.Key.Equals(key));
            // 한 출처가 여러 스탯을 바꿔도 아이콘은 하나. 가장 늦게 끝나는 버프 기준으로 표시한다.
            if (index >= 0 && into[index].EndTime >= b.expireTime) continue;

            var entry = new StatusIconEntry
            {
                Key = key, Icon = state.Icon, EndTime = b.expireTime, Duration = b.duration,
                Count = state.StackCount > 1 ? state.StackCount : 0,
            };
            if (index >= 0) into[index] = entry;
            else into.Add(entry);
        }
    }

    // 읽기 전용. 버프가 없는 스탯은 공용 빈 목록을 돌려주므로 여기에 추가하면 안 된다.
    private List<ActiveBuff> BuffsOf(StatType type) => buffsByType.TryGetValue(type, out var list) ? list : NoBuffs;

    private void Add(ActiveBuff buff)
    {
        activeBuffs.Add(buff);
        if (!buffsByType.TryGetValue(buff.type, out var list))
            buffsByType[buff.type] = list = new List<ActiveBuff>();
        list.Add(buff);
    }

    // match는 같은 버프에 두 번(전체 목록, 스탯별 목록) 쓰이므로 부작용이 없어야 한다.
    private void Remove(System.Predicate<ActiveBuff> match)
    {
        if (activeBuffs.RemoveAll(match) == 0) return;
        foreach (var list in buffsByType.Values)
            list.RemoveAll(match);
    }

    // A buff's conditions may themselves read stats (e.g. HealthRatioCondition -> MaxHealth), which
    // re-enters Apply — so this must never mutate activeBuffs; expired ones are skipped here and
    // pruned in Entity.ApplyBuff instead.
    private bool IsActive(ActiveBuff b, Transform target)
    {
        if (b.expireTime <= Time.time) return false;
        if (b.conditions == null || b.conditions.Count == 0) return true;
        var context = new AbilityContext { Caster = owner, Target = target, State = b.source as AbilityRuntimeState };
        return b.conditions.TrueForAll(c => c == null || c.IsMet(context));
    }
}
