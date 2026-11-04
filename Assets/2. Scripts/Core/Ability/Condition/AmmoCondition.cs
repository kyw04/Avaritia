using System;

// 새 멤버는 맨 뒤에만 추가 (직렬화된 값 보존)
public enum AmmoTarget { Min, Max, Value }

// 새 멤버는 맨 뒤에만 추가 (직렬화된 값 보존)
public enum AmmoComparison { Equal, LessOrEqual, GreaterOrEqual, LessThan, GreaterThan }

public static class AmmoTargetExtensions
{
    public static int Resolve(this AmmoTarget t, IAmmoModule w, int value) => t switch
    {
        AmmoTarget.Min => 0,
        AmmoTarget.Max => w.MaxAmmo,
        _ => value,
    };
}

// 탄약 바 총알 수를 target(최소/최대/지정값)과 비교해 참/거짓을 돌려준다.
// 유지형: OnEquippedTrigger + BuffEffect(duration 0, activeWhile=[AmmoCondition])
[Serializable]
public class AmmoCondition : IAbilityCondition
{
    public AmmoTarget target;
    public AmmoComparison comparison;
    public int value;

    public bool IsMet(AbilityContext context)
    {
        if (context.Caster is not Player { Weapon: { } weapon } || !weapon.TryGetModule<IAmmoModule>(out var w)) return false;
        int current = w.CurrentAmmo;
        int t = target.Resolve(w, value);
        return comparison switch
        {
            AmmoComparison.Equal => current == t,
            AmmoComparison.LessOrEqual => current <= t,
            AmmoComparison.GreaterOrEqual => current >= t,
            AmmoComparison.LessThan => current < t,
            AmmoComparison.GreaterThan => current > t,
            _ => false,
        };
    }
}
