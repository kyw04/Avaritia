using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 전기 팔 모듈: 탄약(스택)이 가득 찬 상태에서 아래 공격이 적중하면 스택을 모두 소모하고,
// 맞은 적에서 시작해 주변 적에게 차례로 피해와 스턴을 건다.
[Serializable]
public class ChainLightningModule : WeaponModule, IObserver<EntityDamagedEvent>
{
    [SerializeField, Min(0)] private int jumps = 5;
    [SerializeField, Min(0)] private float radius = 5f;
    [SerializeField, Min(0)] private float damageMultiplier = 0.7f;
    [SerializeField, Min(0)] private float stunDuration = 0.7f;
    [SerializeField, Min(0)] private float jumpInterval = 0.08f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LineRenderer arcPrefab;
    [SerializeField, Min(0)] private float arcDuration = 0.15f;

    private bool downAttackHeld;
    private bool isDownAttack;
    private Coroutine chainRoutine;
    private IAmmoModule pausedAmmo;

    public override void Equip(ImageCountBar countBar)
    {
        EventBus.Subscribe<EntityDamagedEvent>(this);
    }

    public override void Unequip()
    {
        EventBus.UnsubscribeAll(this);
        if (chainRoutine != null)
        {
            Owner.Mono.StopCoroutine(chainRoutine);
            chainRoutine = null;
        }
        ReleaseAmmo();
        downAttackHeld = false;
        isDownAttack = false;
    }

    public override bool BeforeAttack(WeaponAttack attack)
    {
        isDownAttack = downAttackHeld;
        return true;
    }

    [InputMethod]
    private void SetDownAttackHeld(InputAction.CallbackContext context) =>
        downAttackHeld = context.phase != InputActionPhase.Canceled;

    public void OnNotify(EntityDamagedEvent e)
    {
        if (e.Attacker != Owner || e.Source is not Enemy enemy || enemy.IsAlly || enemy.CurrentHealth <= 0)
            return;
        if (!isDownAttack || chainRoutine != null)
            return;
        if (!Weapon.TryGetModule<IAmmoModule>(out var ammo) || ammo.CurrentAmmo < ammo.MaxAmmo)
            return;

        // 같은 공격의 다른 히트로 중복 발동하지 않게 한다.
        isDownAttack = false;
        ammo.Consume();
        ammo.GainPaused = true;
        pausedAmmo = ammo;
        chainRoutine = Owner.Mono.StartCoroutine(Chain(enemy));
    }

    private IEnumerator Chain(Enemy first)
    {
        var current = first;
        var visited = new HashSet<Enemy> { first };

        for (int i = 0; i <= jumps; i++)
        {
            if (current == null || !current.gameObject.activeInHierarchy || current.CurrentHealth <= 0)
                break;

            current.TakeDamage(Owner.GetDamageAgainst(current.transform) * damageMultiplier, Owner);
            current.Stun(stunDuration);

            if (i == jumps)
                break;

            var next = FindNext(current, visited);
            if (next == null)
                break;

            if (arcPrefab != null)
                SpawnArc(current.transform.position, next.transform.position);

            yield return new WaitForSeconds(jumpInterval);

            visited.Add(next);
            current = next;
        }

        chainRoutine = null;
        ReleaseAmmo();
    }

    private Enemy FindNext(Enemy from, HashSet<Enemy> visited)
    {
        Enemy best = null;
        float bestDist = float.MaxValue;
        foreach (var hit in Physics2D.OverlapCircleAll(from.transform.position, radius, enemyLayer))
        {
            if (!hit.TryGetComponent(out Enemy candidate) || visited.Contains(candidate) || candidate.IsAlly || candidate.CurrentHealth <= 0)
                continue;

            float dist = ((Vector2)(candidate.transform.position - from.transform.position)).sqrMagnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = candidate;
            }
        }
        return best;
    }

    private void SpawnArc(Vector3 from, Vector3 to)
    {
        var arc = UnityEngine.Object.Instantiate(arcPrefab);
        arc.positionCount = 2;
        arc.SetPosition(0, from);
        arc.SetPosition(1, to);
        UnityEngine.Object.Destroy(arc.gameObject, arcDuration);
    }

    private void ReleaseAmmo()
    {
        if (pausedAmmo != null)
            pausedAmmo.GainPaused = false;
        pausedAmmo = null;
    }
}
