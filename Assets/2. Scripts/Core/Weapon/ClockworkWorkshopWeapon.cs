using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 태엽 공방 무기: 아래키를 누르고 있으면 정비 키트를 만들고, 키트를 가진 채 적을 때리면 스턴,
// 스턴된 적을 아래 공격으로 때리면 키트 1개를 소모해 아군으로 만든다.
// 스턴을 걸면 맨 앞 키트 이미지가 바뀌고, 키트를 소모하면 원래대로 돌아온다.
[CreateAssetMenu(menuName = "Scriptable Objects/Weapon/ClockworkWorkshopWeapon")]
public class ClockworkWorkshopWeapon : InputWeapon, IObserver<EntityDamagedEvent>, IObserver<StageNodeChangedEvent>
{
    [SerializeField] private ImageCountPoolable kitImagePrefab;
    [SerializeField] private Sprite stunKitSprite;
    [SerializeField, Min(0)] private int maxKits = 2;
    [SerializeField, Min(1)] private int hitsToStun = 3;
    [SerializeField, Min(0)] private float stunDuration = 7f;
    [SerializeField, Min(0)] private int maxNormalAlliesPerRoom = 3;
    [SerializeField, Min(0)] private int maxLargeAlliesPerRoom = 2;
    [SerializeField, Range(0f, 1f)] private float allyStatMultiplier = 0.8f;

    private ImageCountBar kitCountBar;
    private int kitCount;
    private bool kitStunned;
    private bool downAttackHeld;
    private bool isDownAttack;
    private readonly Dictionary<Enemy, int> hitCounts = new();
    private readonly Dictionary<EnemySize, int> convertedCounts = new();

    public override void Equip(Entity owner, ImageCountBar countBar = null)
    {
        base.Equip(owner, countBar);
        EventBus.Subscribe<EntityDamagedEvent>(this);
        EventBus.Subscribe<StageNodeChangedEvent>(this);

        kitCountBar = countBar;
        kitCountBar?.Init(kitImagePrefab, () => kitCount, () => maxKits, i => i == 0 && kitStunned ? stunKitSprite : null);
        kitCountBar?.Activate();
    }

    public override void Unequip()
    {
        base.Unequip();
        EventBus.UnsubscribeAll(this);
        kitCountBar?.Deactivate();
        downAttackHeld = false;
    }

    public override void Attack(IAttacker attacker, int index = 0, Transform defender = null)
    {
        // 아래 공격 입력을 누른 채 시작한 공격만 스턴된 적을 아군으로 만든다.
        isDownAttack = downAttackHeld;
        base.Attack(attacker, index, defender);
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
