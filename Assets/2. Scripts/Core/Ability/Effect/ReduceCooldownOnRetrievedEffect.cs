using System.Collections.Generic;
using UnityEngine;

// 이 스킬로 내보낸 오브젝트(retrievablePrefab)를 시전자가 다시 얻으면 남은 쿨타임을 reduction초만큼 줄인다.
// 스킬 cooldown이 음수(잠금)면 얻는 순간 바로 풀린다. 오브젝트를 실제로 내보내는 효과(AttackEffect 등)와 같은 스킬에 함께 넣는다.
[System.Serializable]
public class ReduceCooldownOnRetrievedEffect : IAbilityEffect
{
    public GameObject retrievablePrefab;
    public float reduction = 2f;

    // 회수 전에 다시 발동되면(쿨타임이 왕복보다 짧을 때) 이전 대기를 지워 한 번의 회수로 두 번 줄지 않게 한다.
    private static readonly Dictionary<AbilityRuntimeState, System.Action<Entity, GameObject>> pending = new();

    // 도메인 리로드가 꺼져 있어 static 값이 Play 세션 사이에 남으므로 매 Play 시작 시 비운다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => pending.Clear();

    public void Apply(AbilityContext context)
    {
        if (retrievablePrefab == null || context.Caster is not Entity caster || context.State == null) return;

        var state = context.State;
        var prefab = retrievablePrefab;
        float seconds = reduction;

        if (pending.Remove(state, out var previous))
            ObjectRetrieval.Retrieved -= previous;

        System.Action<Entity, GameObject> onRetrieved = null;
        onRetrieved = (owner, retrieved) =>
        {
            if (owner != caster || retrieved != prefab) return;
            ObjectRetrieval.Retrieved -= onRetrieved;
            pending.Remove(state);
            Reduce(caster, state, seconds);
        };
        pending[state] = onRetrieved;
        ObjectRetrieval.Retrieved += onRetrieved;
    }

    private static void Reduce(Entity caster, AbilityRuntimeState state, float seconds)
    {
        if (caster.Abilities != null && caster.Abilities.ReduceCooldown(state, seconds)) return;

        // 스킬 슬롯 매니저에 없는 바인딩(아이템/무기 어빌리티)이면 UI 이벤트 없이 상태만 바꾼다.
        bool locked = state.CooldownEndTime == float.MaxValue;
        state.CooldownEndTime = locked ? Time.time : Mathf.Max(Time.time, state.CooldownEndTime - seconds);
    }
}
