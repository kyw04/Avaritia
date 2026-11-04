using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 스택 무기: 공격하면 스택이 쌓이고(탄약 바의 총알 수로 표시), 공격을 멈추면 천천히 줄어든다.
// 최대 스택에서 또 쌓으려 하면 과열되어 자신이 피해를 입는다.
// 스택 단계별 버프는 passiveAbilities에 AmmoCondition / OnAmmoReachedTrigger로 붙인다.
[CreateAssetMenu(menuName = "Scriptable Objects/Weapon/StackWeapon")]
public class StackWeapon : Weapon, IAmmoWeapon, IObserver<EntityDamagedEvent>
{
    [SerializeField] private ImageCountPoolable stackImagePrefab;
    [SerializeField, Min(1)] private int maxStack = 10;
    [SerializeField] private StackGainMode gainMode;
    [SerializeField] private List<Enemy> targetEnemyPrefabs = new();
    [SerializeField, Min(0)] private float decayDelay = 1f;
    [SerializeField, Min(0)] private float decayInterval = 0.3f;
    [SerializeField, Min(0)] private float overflowSelfDamage = 0f;
    [SerializeField] private PooledParticle overflowParticle;

    private ImageCountBar stackCountBar;
    private int stack;
    private float lastAttackTime;
    private Enemy sameEnemyTarget;
    private Coroutine decayRoutine;

    public int CurrentAmmo => stack;
    public int MaxAmmo => maxStack;
    public event Action<int, int> AmmoChanged;

    public override void Equip(Entity owner, ImageCountBar countBar = null)
    {
        base.Equip(owner, countBar);
        stack = 0;
        sameEnemyTarget = null;
        EventBus.Subscribe<EntityDamagedEvent>(this);

        stackCountBar = countBar;
        stackCountBar?.Init(stackImagePrefab, () => stack, () => maxStack);
        stackCountBar?.Activate();
    }

    public override void Unequip()
    {
        base.Unequip();
        EventBus.UnsubscribeAll(this);
        if (decayRoutine != null)
        {
            Owner.Mono.StopCoroutine(decayRoutine);
            decayRoutine = null;
        }
        // 의도적으로 SetStack을 거치지 않는다: AmmoChanged가 발생하면 해제 중인 이전 무기의 총알 도달 트리거가 발동한다.
        stack = 0;
        sameEnemyTarget = null;
        stackCountBar?.Deactivate();
    }

    public override void Attack(IAttacker attacker, int index = 0, Transform defender = null)
    {
        lastAttackTime = Time.time;
        if (gainMode == StackGainMode.OnSwing)
            TryGain();
        RestartDecay();
        base.Attack(attacker, index, defender);
    }

    public void OnNotify(EntityDamagedEvent e)
    {
        if (e.Attacker != Owner || e.Source is not Enemy enemy)
            return;

        switch (gainMode)
        {
            case StackGainMode.OnHit:
                TryGain();
                break;
            case StackGainMode.OnSameEnemyHit:
                if (sameEnemyTarget == null || sameEnemyTarget == enemy)
                {
                    sameEnemyTarget = enemy;
                    TryGain();
                }
                else
                {
                    // SetStack(0)이 기준 적을 비우므로 그 뒤에 새 기준 적을 대입한다.
                    SetStack(0);
                    sameEnemyTarget = enemy;
                }
                break;
            case StackGainMode.OnSpecificEnemyHit:
                if (IsTargetEnemy(enemy))
                    TryGain();
                break;
        }
    }

    private void TryGain()
    {
        if (stack < maxStack)
        {
            SetStack(stack + 1);
            // 히트 계열 모드는 스택이 0일 때 감소 코루틴이 이미 끝나 있으므로 다시 시작한다.
            RestartDecay();
            return;
        }

        if (overflowSelfDamage <= 0f)
            return;

        Owner.TakeDamage(overflowSelfDamage);
        if (overflowParticle != null)
            ParticleManager.Instance.Spawn(overflowParticle, Owner.transform);
    }

    private void SetStack(int value)
    {
        value = Mathf.Clamp(value, 0, maxStack);
        if (value != stack)
        {
            int prev = stack;
            stack = value;
            stackCountBar?.Refresh();
            AmmoChanged?.Invoke(prev, stack);
        }

        if (stack == 0)
            sameEnemyTarget = null;
    }

    private bool IsTargetEnemy(Enemy enemy)
    {
        var prefab = ObjectPoolManager.Instance.GetPrefab(enemy.gameObject);
        foreach (var p in targetEnemyPrefabs)
        {
            if (p == null)
                continue;
            if (prefab != null ? p.gameObject == prefab : enemy.name.StartsWith(p.name))
                return true;
        }
        return false;
    }

    private void RestartDecay()
    {
        if (decayRoutine != null)
            Owner.Mono.StopCoroutine(decayRoutine);
        decayRoutine = Owner.Mono.StartCoroutine(Decay());
    }

    private IEnumerator Decay()
    {
        while (stack > 0)
        {
            yield return new WaitUntil(() => Time.time >= lastAttackTime + decayDelay);
            yield return new WaitForSeconds(decayInterval);
            // 대기 중 다시 공격했다면 이번 감소는 건너뛴다.
            if (Time.time >= lastAttackTime + decayDelay)
                SetStack(stack - 1);
        }
        decayRoutine = null;
    }
}

public enum StackGainMode
{
    OnSwing,
    OnHit,
    OnSameEnemyHit,
    OnSpecificEnemyHit,
    // 직렬화 값이므로 새 항목은 맨 뒤에만 추가한다.
}
