using System.Collections.Generic;
using UnityEngine;

public enum DamageOutcome
{
    Ignored,  // 무적
    Evaded,
    Blocked,  // 피격 차단
    Absorbed, // 실드가 다 막음
    Damaged,
}

// 엔티티가 피해를 받기 전 단계: 무적, 회피, 피격 차단, 방어구 감소, 실드 흡수와 경직 면역을 맡는다.
// 체력 감소, 이벤트 발행, 사망은 Entity가 한다.
public class DamageReceiver
{
    private readonly Entity owner;
    // Keyed by the granting binding's AbilityRuntimeState (same as buff sources), so each copy
    // of an item refills/loses only its own share and RemoveSource can drop it on unequip.
    private readonly Dictionary<object, float> shields = new();
    private readonly Dictionary<object, int> hitBlocks = new();
    private readonly List<object> keyBuffer = new();
    private float invincibleUntil;
    private float invincibleDuration;
    private Sprite invincibleIcon;
    private float noHitStunUntil;
    private float noHitStunDuration;
    private Sprite noHitStunIcon;

    public DamageReceiver(Entity owner) => this.owner = owner;

    public bool IgnoresHitStun => Time.time < noHitStunUntil;

    // Refills (not adds to) this source's share — re-granting every room shouldn't stack up.
    public void GrantShield(object source, float amount) => shields[source] = amount;

    public void GrantHitBlocks(object source, int count) => hitBlocks[source] = count;

    public bool HasShield(object source) => shields.TryGetValue(source, out var amount) && amount > 0f;

    public bool HasHitBlocks(object source) => hitBlocks.TryGetValue(source, out var count) && count > 0;

    public void RemoveSource(object source)
    {
        shields.Remove(source);
        hitBlocks.Remove(source);
    }

    // icon: 상태 아이콘 UI에 띄울 아이콘(null이면 표시 안 함). 더 길게 남는 쪽의 아이콘/길이를 유지한다.
    public void SetInvincible(float duration, Sprite icon)
    {
        float until = Time.time + duration;
        if (until <= invincibleUntil) return;
        invincibleUntil = until;
        invincibleDuration = duration;
        invincibleIcon = icon;
    }

    // SetInvincible과 같은 규칙: 더 길게 남는 쪽의 아이콘/길이를 유지한다. duration 0 = 만료되지 않음.
    public void SetNoHitStun(float duration, Sprite icon)
    {
        float until = duration > 0f ? Time.time + duration : float.MaxValue;
        if (until <= noHitStunUntil) return;
        noHitStunUntil = until;
        noHitStunDuration = duration;
        noHitStunIcon = icon;
    }

    // 리셋 때 어빌리티를 다시 바인딩하기 전에 비운다(장착 즉시 주는 실드·차단은 바인딩 뒤에 다시 들어온다).
    public void ClearGrants()
    {
        shields.Clear();
        hitBlocks.Clear();
        noHitStunUntil = 0f;
        noHitStunIcon = null;
    }

    public void ClearInvincible()
    {
        invincibleUntil = 0f;
        invincibleIcon = null;
    }

    // 실제로 받을 피해를 계산한다. Damaged가 아니면 피해를 받지 않는다.
    public float Resolve(DamageInfo info, out DamageOutcome outcome)
    {
        if (Time.time < invincibleUntil)
        {
            outcome = DamageOutcome.Ignored;
            return 0f;
        }
        if (!info.Has(DamageFlags.IgnoreEvasion) && Random.Range(0f, 100f) < owner.GetStat<float>(StatType.Evasion))
        {
            outcome = DamageOutcome.Evaded;
            return 0f;
        }
        if (TryConsumeHitBlock())
        {
            outcome = DamageOutcome.Blocked;
            return 0f;
        }

        float damage = info.Amount;
        // Armor = 받는 피해 감소율(%). 최대 80%까지, 음수면 피해 증가 (-10 = 1.1배).
        if (!info.Has(DamageFlags.IgnoreArmor))
        {
            owner.TryGetStat<int>(StatType.Armor, out var armor);
            damage *= 1f - Mathf.Min(armor, 80) / 100f;
        }
        damage = AbsorbWithShields(damage);
        outcome = damage > 0f ? DamageOutcome.Damaged : DamageOutcome.Absorbed;
        return damage;
    }

    // 상태 아이콘 UI용: 실드/무효화/무적/경직 면역. 아이콘을 알 수 없는 출처는 건너뛴다.
    public void CollectStatusIcons(List<StatusIconEntry> into)
    {
        foreach (var pair in shields)
            if (pair.Value > 0f && pair.Key is AbilityRuntimeState state && state.Icon != null)
                into.Add(new StatusIconEntry
                {
                    Key = (pair.Key, StatusIconKind.Shield), Icon = state.Icon, EndTime = float.MaxValue,
                    Count = Mathf.CeilToInt(pair.Value),
                });

        foreach (var pair in hitBlocks)
            if (pair.Value > 0 && pair.Key is AbilityRuntimeState state && state.Icon != null)
                into.Add(new StatusIconEntry
                {
                    Key = (pair.Key, StatusIconKind.HitBlock), Icon = state.Icon, EndTime = float.MaxValue,
                    Count = pair.Value,
                });

        if (invincibleIcon != null && Time.time < invincibleUntil)
            into.Add(new StatusIconEntry
            {
                Key = (owner, StatusIconKind.Invincible), Icon = invincibleIcon,
                EndTime = invincibleUntil, Duration = invincibleDuration,
            });

        if (noHitStunIcon != null && IgnoresHitStun)
            into.Add(new StatusIconEntry
            {
                Key = (owner, StatusIconKind.NoHitStun), Icon = noHitStunIcon,
                EndTime = noHitStunUntil, Duration = noHitStunDuration,
            });
    }

    // 딕셔너리를 돌면서 값을 바꿀 수 없어 키를 버퍼에 옮겨 돈다. 두 메서드는 안에서 외부 코드를 부르지 않아 버퍼를 같이 쓴다.
    private bool TryConsumeHitBlock()
    {
        keyBuffer.Clear();
        keyBuffer.AddRange(hitBlocks.Keys);
        foreach (var source in keyBuffer)
        {
            if (hitBlocks[source] <= 0) continue;
            hitBlocks[source]--;
            return true;
        }
        return false;
    }

    private float AbsorbWithShields(float damage)
    {
        keyBuffer.Clear();
        keyBuffer.AddRange(shields.Keys);
        foreach (var source in keyBuffer)
        {
            if (damage <= 0f) break;
            float absorbed = Mathf.Min(shields[source], damage);
            shields[source] -= absorbed;
            damage -= absorbed;
        }
        return damage;
    }
}
