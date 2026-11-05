using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 자석 모듈: 앞쪽 범위의 적을 밀쳐내거나(밀치기) 빨아들인다(흡인). 일반 크기 Enemy만 영향을 받는다.
// 입력은 두 방식 중 하나로 연결한다.
//  - 통합: OnDownAttack을 DownAttack Started/Canceled에 연결. pullDelay 전에 떼면 밀치기, 더 누르면 흡인.
//  - 분리: Push(밀치기), StartPull(흡인 시작, 홀드 바인딩), StopPull(흡인 끝)을 각각 연결.
// 흡인은 maxPullDuration이 지나거나 놓으면 끝나고, 끌려온 적은 그때 기절한다.
// 쿨타임은 입력마다 InputCooldownModule(startOnInput 끔)로 건다. 밀치기는 쓰는 순간, 흡인은 끝나는 순간 그 입력의 쿨이 시작된다.
// 통합 입력은 입력이 하나라 밀치기/흡인이 쿨을 함께 쓴다. 따로 돌리려면 분리 입력으로 연결한다.
// 밀려난 적은 pushVelocity 방향(대각선)으로 최고 속도로 튀어 나가 pushDuration 동안 점점 느려진다. 날아가는 동안은 기절 상태다.
// 밀친 뒤 slamWindowDuration 안에 slamLayer(벽)에 닿으면 넉백 없이 추가 피해와 스턴을 받고,
// 그 뒤에 닿으면 판정 없이 멈춘다. 밀친 직후 slamGraceDuration 동안은 벽에 닿아도 무시한다.
// 자석을 쓰는 동안에는 같은 키의 근접 공격이 나가지 않는다.
[Serializable]
public class MagnetModule : WeaponModule
{
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask slamLayer;
    // 바라보는 방향 기준 범위 중심(x는 앞쪽)과 크기.
    [SerializeField] private Vector2 areaOffset = new(2f, 0f);
    [SerializeField] private Vector2 areaSize = new(4f, 2f);
    [SerializeField, Min(0)] private float pullDelay = 0.25f;
    // 최고 속도. x는 앞쪽, y는 위쪽. 날아가는 동안 방향은 그대로 두고 크기만 바뀐다.
    [SerializeField] private Vector2 pushVelocity = new(18f, 3f);
    [SerializeField, Min(0)] private float pushDuration = 0.8f;
    [SerializeField, Min(0)] private float pushDamageMultiplier;
    [SerializeField, Min(0)] private float slamWindowDuration = 0.7f;
    // slamWindowDuration보다 짧아야 벽 판정이 날 수 있다.
    [SerializeField, Min(0)] private float slamGraceDuration = 0.1f;
    [SerializeField, Min(0)] private float slamDamageMultiplier = 1.5f;
    [SerializeField, Min(0)] private float slamStunDuration = 1.5f;
    [SerializeField, Min(0)] private float pullSpeed = 10f;
    [SerializeField, Min(0)] private float pullStopDistance = 1.2f;
    [SerializeField, Min(0)] private float maxPullDuration = 3f;
    [SerializeField, Min(0)] private float pullReleaseStunDuration = 1f;
    // 기절 디버프 에셋(Debuff_Stun). 지속 시간은 각 시간에 건 쪽의 DebuffDuration 보너스가 더해진다.
    [SerializeField] private DebuffData stunDebuff;
    [SerializeField] private ActionFeedback pushFeedback;
    [SerializeField] private ActionFeedback pullFeedback;
    [SerializeField] private ActionFeedback slamFeedback;

    // 통합 입력에서 누르고 있는 동안. pullUsed는 이번 누름에서 흡인을 이미 썼는지(그러면 놓을 때 밀치지 않는다).
    private bool pressing;
    private bool pullUsed;
    private Coroutine pressRoutine;
    private Coroutine pullRoutine;
    // 흡인이 끝날 때 쿨타임을 시작할 입력 메서드(통합이면 OnDownAttack, 분리면 StartPull).
    private string pullInputMethod;
    private bool meleeBlocked;
    private float savedAttackReadyTime;
    private HashSet<Enemy> pulledEnemies = new();
    // 같은 적을 다시 밀면 이전 벽 충돌 감시를 멈춰 한 번만 박히게 한다.
    private Dictionary<Enemy, Coroutine> slamRoutines = new();

    private bool IsPulling => pullRoutine != null;

    public override WeaponModule CreateRuntimeCopy()
    {
        var copy = (MagnetModule)base.CreateRuntimeCopy();
        copy.pulledEnemies = new HashSet<Enemy>();
        copy.slamRoutines = new Dictionary<Enemy, Coroutine>();
        return copy;
    }

    public override void Unequip()
    {
        EndPull();
        EndPress();
    }

    // 통합 입력. DownAttack 액션의 Started/Canceled에 연결한다.
    // 쿨타임 중에는 Started가 실행되지 않아 근접 공격이 그대로 나간다.
    [InputMethod]
    private void OnDownAttack(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            if (pressing || IsPulling)
                return;

            pressing = true;
            pullUsed = false;
            BlockMelee();
            pressRoutine = Owner.StartCoroutine(PressRoutine());
        }
        else if (context.phase == InputActionPhase.Canceled && pressing)
        {
            if (IsPulling)
                EndPull();
            else if (!pullUsed)
                PushAndCooldown(nameof(MagnetModule) + "." + nameof(OnDownAttack));
            EndPress();
        }
    }

    private IEnumerator PressRoutine()
    {
        yield return new WaitForSeconds(pullDelay);
        pressRoutine = null;
        pullUsed = true;
        BeginPull(nameof(MagnetModule) + "." + nameof(OnDownAttack));
    }

    private void EndPress()
    {
        if (!pressing)
            return;
        if (pressRoutine != null)
            Owner.StopCoroutine(pressRoutine);
        pressRoutine = null;
        pressing = false;
        UnblockMelee();
    }

    // 분리 입력: 누르는 즉시 밀친다. 같은 프레임에 같은 키로 시작될 근접 공격은 막는다.
    [InputMethod]
    private void Push()
    {
        if (pressing || IsPulling)
            return;

        PushAndCooldown(nameof(MagnetModule) + "." + nameof(Push));
        if (Owner is Player player && !meleeBlocked)
            player.AttackReadyTime = Mathf.Max(player.AttackReadyTime, Time.time + 0.05f);
    }

    // 분리 입력: 홀드 바인딩(holdDuration)에 연결한다.
    [InputMethod]
    private void StartPull()
    {
        if (pressing || IsPulling)
            return;
        BlockMelee();
        BeginPull(nameof(MagnetModule) + "." + nameof(StartPull));
    }

    // 분리 입력: 홀드 키의 Canceled에 연결한다.
    [InputMethod]
    private void StopPull() => EndPull();

    private void BeginPull(string inputMethod)
    {
        pullInputMethod = inputMethod;
        pulledEnemies.Clear();
        pullFeedback.Play(Owner.transform.position);
        pullRoutine = Owner.StartCoroutine(PullRoutine());
    }

    private IEnumerator PullRoutine()
    {
        for (float elapsed = 0f; elapsed < maxPullDuration; elapsed += Time.fixedDeltaTime)
        {
            yield return new WaitForFixedUpdate();
            foreach (var enemy in FindTargets())
            {
                pulledEnemies.Add(enemy);
                Vector2 toOwner = (Vector2)Owner.transform.position - (Vector2)enemy.transform.position;
                Hold(enemy, toOwner.magnitude <= pullStopDistance ? Vector2.zero : toOwner.normalized * pullSpeed);
            }
        }
        pullRoutine = null;
        EndPull(timedOut: true);
    }

    // 놓거나 시간이 다 되면 끌려온 적을 기절시키고 흡인 쿨타임을 시작한다.
    // 통합 입력에서 시간이 다 됐으면 놓을 때까지 근접 공격을 계속 막는다.
    private void EndPull(bool timedOut = false)
    {
        if (!timedOut && pullRoutine == null)
            return;
        if (pullRoutine != null)
            Owner.StopCoroutine(pullRoutine);
        pullRoutine = null;

        foreach (var enemy in pulledEnemies)
        {
            if (IsAlive(enemy))
                enemy.Debuffs.Apply(stunDebuff, pullReleaseStunDuration, Owner);
        }
        pulledEnemies.Clear();
        Weapon.StartInputCooldown(pullInputMethod);
        if (!pressing)
            UnblockMelee();
    }

    private void PushAndCooldown(string inputMethod)
    {
        DoPush();
        Weapon.StartInputCooldown(inputMethod);
    }

    // InputHandler는 같은 X 입력으로 Update에서 근접 공격을 시작하려 하므로 CanAttack을 막아둔다.
    private void BlockMelee()
    {
        if (meleeBlocked || Owner is not Player player)
            return;

        meleeBlocked = true;
        savedAttackReadyTime = player.AttackReadyTime;
        player.AttackReadyTime = float.PositiveInfinity;
    }

    private void UnblockMelee()
    {
        if (!meleeBlocked)
            return;

        meleeBlocked = false;
        if (Owner is Player player)
            player.AttackReadyTime = savedAttackReadyTime;
    }

    // 피해를 먼저 줘서 무기 넉백이 걸려도 밀치기 속도로 덮어쓴다.
    private void DoPush()
    {
        pushFeedback.Play(Owner.transform.position);
        float direction = Owner.LookDirection;

        foreach (var enemy in FindTargets())
        {
            if (pushDamageMultiplier > 0f)
                DealDamage(enemy, pushDamageMultiplier);
            if (!IsAlive(enemy))
                continue;

            enemy.ApplyKnockback(PushVelocity(direction), Time.fixedDeltaTime * 2f);
            // 기절은 이동 입력만 막고 넉백은 그대로 받아 날아가는 속도를 유지한다.
            enemy.Debuffs.Apply(stunDebuff, pushDuration, Owner);
            if (slamRoutines.Remove(enemy, out var previous) && previous != null)
                enemy.StopCoroutine(previous);
            // 적에서 돌려 무기를 바꾸거나 버려도 날아가는 적은 그대로 벽에 박힐 수 있다.
            slamRoutines[enemy] = enemy.StartCoroutine(Fly(enemy, direction));
        }
    }

    // 날아가는 동안 매 물리 프레임 속도를 정해 중력 없이 같은 방향으로 날아가게 한다. 넉백 저항이 있으면 ApplyKnockback이 그만큼 줄인다.
    private IEnumerator Fly(Enemy enemy, float direction)
    {
        for (float elapsed = 0f; elapsed < pushDuration; elapsed += Time.fixedDeltaTime)
        {
            yield return new WaitForFixedUpdate();
            if (!IsAlive(enemy) || !enemy.IsKnockedBack)
                break;

            if (elapsed >= slamGraceDuration && enemy.Rb.IsTouchingLayers(slamLayer))
            {
                if (elapsed < slamWindowDuration)
                    Slam(enemy);
                break;
            }

            enemy.ApplyKnockback(PushVelocity(direction) * (1f - elapsed / pushDuration), Time.fixedDeltaTime * 2f);
        }
        slamRoutines.Remove(enemy);
    }

    private Vector2 PushVelocity(float direction) => new(pushVelocity.x * direction, pushVelocity.y);

    private void Slam(Enemy enemy)
    {
        DealDamage(enemy, slamDamageMultiplier);
        // 피해를 받으면 Player가 무기 넉백을 걸므로 바로 끊는다.
        enemy.ApplyKnockback(Vector2.zero, 0f);
        if (IsAlive(enemy))
            enemy.Debuffs.Apply(stunDebuff, slamStunDuration, Owner);
        slamFeedback.Play(enemy.transform.position);
    }

    private List<Enemy> FindTargets()
    {
        var center = (Vector2)Owner.transform.position + new Vector2(areaOffset.x * Owner.LookDirection, areaOffset.y);
        var targets = new List<Enemy>();
        foreach (var collider in Physics2D.OverlapBoxAll(center, areaSize, 0f, enemyLayer))
        {
            var enemy = collider.GetComponentInParent<Enemy>();
            if (enemy is { IsAlly: false, Size: EnemySize.Normal } && IsAlive(enemy) && !targets.Contains(enemy))
                targets.Add(enemy);
        }
        return targets;
    }
}
