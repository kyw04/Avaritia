using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 태엽 공방 무기: 아래키를 누르고 있으면 정비 키트를 만들고, 키트를 가진 채 적을 때리면 스턴,
// 스턴된 적을 아래 공격으로 때리면 키트 1개를 소모해 아군으로 만든다.
[CreateAssetMenu(menuName = "Scriptable Objects/Weapon/ClockworkWorkshopWeapon")]
public class ClockworkWorkshopWeapon : InputWeapon, IObserver<EntityDamagedEvent>, IObserver<StageNodeChangedEvent>
{
    [Header("정비 키트")]
    [SerializeField] private ImageCountPoolable kitImagePrefab;
    [SerializeField, Min(0)] private float kitChargeTime = 3f;
    [SerializeField, Min(0)] private int maxKits = 2;

    [Header("스턴")]
    [SerializeField, Min(1)] private int hitsToStun = 3;
    [SerializeField, Min(0)] private float stunDuration = 7f;

    [Header("아군 전환 (방마다 초기화)")]
    [SerializeField, Min(0)] private int maxNormalAlliesPerRoom = 3;
    [SerializeField, Min(0)] private int maxLargeAlliesPerRoom = 2;
    [SerializeField, Range(0f, 1f)] private float allyStatMultiplier = 0.8f;

    private ImageCountBar kitCountBar;
    private int kitCount;
    private Coroutine chargeCoroutine;
    private bool isDownAttack;
    private readonly Dictionary<Enemy, int> hitCounts = new();
    private readonly Dictionary<EnemySize, int> convertedCounts = new();

    public override void Equip(Entity owner, ImageCountBar countBar = null)
    {
        base.Equip(owner, countBar);
        EventBus.Subscribe<EntityDamagedEvent>(this);
        EventBus.Subscribe<StageNodeChangedEvent>(this);

        kitCountBar = countBar;
        kitCountBar?.Init(kitImagePrefab, () => kitCount, () => maxKits);
        kitCountBar?.Activate();
    }

    public override void Unequip()
    {
        base.Unequip();
        EventBus.UnsubscribeAll(this);
        kitCountBar?.Deactivate();
        StopCharging();
    }

    public override void Attack(IAttacker attacker, int index = 0, Transform defender = null)
    {
        // 아래키를 누른 채 시작한 공격만 스턴된 적을 아군으로 만든다.
        isDownAttack = InputHandler.Instance.MoveInput.y < -0.5f;
        base.Attack(attacker, index, defender);
    }

    protected override void OnInput(InputAction.CallbackContext context)
    {
        if (context.action != Gameplay.Move)
            return;

        if (context.ReadValue<Vector2>().y < -0.5f)
            StartCharging();
        else
            StopCharging();
    }

    private void StartCharging()
    {
        if (chargeCoroutine != null)
            return;

        chargeCoroutine = Owner.Mono.StartCoroutine(ChargeKits());
    }

    private void StopCharging()
    {
        if (chargeCoroutine == null)
            return;

        Owner.Mono.StopCoroutine(chargeCoroutine);
        chargeCoroutine = null;
    }

    // 아래키를 누르고 있는 동안 kitChargeTime마다 정비 키트를 하나씩 만든다. 가득 차 있으면 빌 때까지 기다린다.
    private IEnumerator ChargeKits()
    {
        while (true)
        {
            yield return new WaitUntil(() => kitCount < maxKits);
            yield return new WaitForSeconds(kitChargeTime);
            kitCount++;
            kitCountBar?.Refresh();
        }
    }

    public void OnNotify(EntityDamagedEvent e)
    {
        if (e.Attacker != Owner || e.Source is not Enemy enemy || enemy.IsAlly || enemy.CurrentHealth <= 0)
            return;
        if (kitCount <= 0)
            return;

        if (enemy.IsStunned)
        {
            if (isDownAttack)
                TryConvert(enemy);
            return;
        }

        hitCounts.TryGetValue(enemy, out int hits);
        if (++hits < hitsToStun)
        {
            hitCounts[enemy] = hits;
            return;
        }

        hitCounts.Remove(enemy);
        enemy.Stun(stunDuration);
    }

    private void TryConvert(Enemy enemy)
    {
        convertedCounts.TryGetValue(enemy.Size, out int converted);
        int max = enemy.Size == EnemySize.Large ? maxLargeAlliesPerRoom : maxNormalAlliesPerRoom;
        if (converted >= max)
            return;

        convertedCounts[enemy.Size] = converted + 1;
        kitCount--;
        kitCountBar?.Refresh();
        enemy.ConvertToAlly(Owner.transform, allyStatMultiplier);
    }

    public void OnNotify(StageNodeChangedEvent e)
    {
        hitCounts.Clear();
        convertedCounts.Clear();
    }
}
