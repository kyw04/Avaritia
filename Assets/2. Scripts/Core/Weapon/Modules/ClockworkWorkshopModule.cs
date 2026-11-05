using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 태엽 공방 모듈: 아래키를 누르고 있으면 정비 키트를 만들고, 키트를 가진 채 적을 때리면 스턴,
// 스턴된 적을 아래 공격으로 때리면 키트 1개를 소모해 아군으로 만든다.
// 스턴을 걸면 맨 앞 키트 이미지가 바뀌고, 키트를 소모하면 원래대로 돌아온다.
[Serializable]
public class ClockworkWorkshopModule : WeaponModule, IObserver<EntityDamagedEvent>, IObserver<StageNodeChangedEvent>
{
    [SerializeField] private ImageCountStyle kitStyle;
    [SerializeField] private GaugeSlotLook stunKitLook = new();
    [SerializeField, Min(0)] private int maxKits = 2;
    [SerializeField, Min(1)] private int hitsToStun = 3;
    // 기절 디버프 에셋(Debuff_Stun). 지속 시간은 아래 값에 건 쪽의 DebuffDuration 보너스가 더해진다.
    [SerializeField] private DebuffData stunDebuff;
    [SerializeField, Min(0)] private float stunDuration = 7f;
    [SerializeField, Min(0)] private int maxNormalAlliesPerRoom = 3;
    [SerializeField, Min(0)] private int maxLargeAlliesPerRoom = 2;
    [SerializeField, Range(0f, 1f)] private float allyStatMultiplier = 0.8f;

    private ImageCountBar kitCountBar;
    private int kitCount;
    private bool kitStunned;
    private bool downAttackHeld;
    private bool isDownAttack;
    private Dictionary<Enemy, int> hitCounts = new();
    private Dictionary<EnemySize, int> convertedCounts = new();

    public override bool UsesCountBar => true;

    public override WeaponModule CreateRuntimeCopy()
    {
        var copy = (ClockworkWorkshopModule)base.CreateRuntimeCopy();
        copy.hitCounts = new Dictionary<Enemy, int>();
        copy.convertedCounts = new Dictionary<EnemySize, int>();
        return copy;
    }

    public override void Equip(ImageCountBar countBar)
    {
        EventBus.Subscribe<EntityDamagedEvent>(this);
        EventBus.Subscribe<StageNodeChangedEvent>(this);

        kitCountBar = countBar;
        kitCountBar?.Init(kitStyle, () => kitCount, () => maxKits, i => i == 0 && kitStunned ? stunKitLook : null);
        kitCountBar?.Activate();
    }

    public override void Unequip()
    {
        EventBus.UnsubscribeAll(this);
        kitCountBar?.Deactivate();
        downAttackHeld = false;
    }

    public override bool BeforeAttack(WeaponAttack attack)
    {
        // 아래 공격 입력을 누른 채 시작한 공격만 스턴된 적을 아군으로 만든다.
        isDownAttack = downAttackHeld;
        return true;
    }

    // DownAttack 액션의 Started/Canceled에 연결한다. InputHandler가 입력 콜백이 모두 끝난 뒤 Update에서 공격을 시작하므로
    // 같은 프레임에 기록한 값이 첫 공격부터 반영된다.
    [InputMethod]
    private void SetDownAttackHeld(InputAction.CallbackContext context) =>
        downAttackHeld = context.phase != InputActionPhase.Canceled;

    [InputMethod]
    private void AddKit()
    {
        if (kitCount >= maxKits)
            return;

        kitCount++;
        kitCountBar?.Refresh();
    }

    public void OnNotify(EntityDamagedEvent e)
    {
        if (e.Attacker != Owner || e.Source is not Enemy enemy || enemy.IsAlly || enemy.CurrentHealth <= 0 || e.Info.Has(DamageFlags.NoOnHitEffects))
            return;
        if (kitCount <= 0)
            return;

        if (enemy.Debuffs.Has(stunDebuff))
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
        enemy.Debuffs.Apply(stunDebuff, stunDuration, Owner);
        kitStunned = true;
        kitCountBar?.Refresh();
    }

    private void TryConvert(Enemy enemy)
    {
        convertedCounts.TryGetValue(enemy.Size, out int converted);
        int max = enemy.Size == EnemySize.Large ? maxLargeAlliesPerRoom : maxNormalAlliesPerRoom;
        if (converted >= max)
            return;

        convertedCounts[enemy.Size] = converted + 1;
        kitCount--;
        kitStunned = false;
        kitCountBar?.Refresh();
        enemy.ConvertToAlly(Owner.transform, allyStatMultiplier);
    }

    public void OnNotify(StageNodeChangedEvent e)
    {
        hitCounts.Clear();
        convertedCounts.Clear();
    }
}
