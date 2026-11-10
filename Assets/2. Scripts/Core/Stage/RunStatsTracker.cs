using System;
using System.Collections.Generic;
using UnityEngine;

// 한 런의 결과 화면용 통계. 게임플레이 코드에 끼어들지 않고 EventBus 구독만으로 모은다.
// 사망 원인과 최종 빌드는 사망 순간의 스냅샷이다.
public class RunStatsTracker : MonoBehaviour,
    IObserver<StageNodeClearedEvent>,
    IObserver<EntityDeadEvent>,
    IObserver<EntityDamagedEvent>,
    IObserver<PlayerDiedEvent>,
    IObserver<RunResetEvent>
{
    public const string UnknownKiller = "알 수 없음";

    [SerializeField] private Player player;

    private readonly List<Sprite> itemIcons = new();
    private float startTime;
    private float endTime = -1f;

    public int RoomsCleared { get; private set; }
    // 사망 후에는 사망 시점에서 멈춘다. 일시정지(timeScale 0) 시간은 들어가지 않는다.
    public float PlayTime => (endTime >= 0f ? endTime : Time.time) - startTime;
    public int Kills { get; private set; }
    public float DamageDealt { get; private set; }
    public float DamageTaken { get; private set; }
    public string KillerName { get; private set; } = UnknownKiller;
    public Sprite KillerSprite { get; private set; }
    public Sprite WeaponIcon { get; private set; }
    public IReadOnlyList<Sprite> ItemIcons => itemIcons;

    // 사망 스냅샷을 저장한 직후 호출된다.
    public event Action Finalized;

    private void Awake()
    {
        EventBus.Subscribe<StageNodeClearedEvent>(this);
        EventBus.Subscribe<EntityDeadEvent>(this);
        EventBus.Subscribe<EntityDamagedEvent>(this);
        EventBus.Subscribe<PlayerDiedEvent>(this);
        EventBus.Subscribe<RunResetEvent>(this);
        ResetStats();
    }

    private void OnDestroy() => EventBus.UnsubscribeAll(this);

    // 처음부터 클리어 상태인 방(시작 방, 상점)은 들어가기만 해도 클리어 이벤트가 나오므로 세지 않는다.
    public void OnNotify(StageNodeClearedEvent e)
    {
        if (e.Node != null && !e.Node.wasCleared)
            RoomsCleared++;
    }

    public void OnNotify(EntityDeadEvent e)
    {
        if (e.Source != player)
            Kills++;
    }

    public void OnNotify(EntityDamagedEvent e)
    {
        if (e.Attacker == player && e.Source != player)
            DamageDealt += e.Amount;
        if (e.Source == player)
            DamageTaken += e.Amount;
    }

    public void OnNotify(PlayerDiedEvent e)
    {
        endTime = Time.time;

        var killer = player.LastAttacker;
        bool hasKiller = killer != null && killer != player;
        KillerName = hasKiller ? killer.DisplayName : UnknownKiller;
        var killerRenderer = hasKiller ? killer.GetComponentInChildren<SpriteRenderer>() : null;
        KillerSprite = killerRenderer != null ? killerRenderer.sprite : null;

        WeaponIcon = player.Weapon?.Data.Icon;
        itemIcons.Clear();
        foreach (var item in player.Inventory.Items)
            if (item.Icon != null)
                itemIcons.Add(item.Icon);

        Finalized?.Invoke();
    }

    public void OnNotify(RunResetEvent e) => ResetStats();

    private void ResetStats()
    {
        RoomsCleared = 0;
        Kills = 0;
        DamageDealt = 0f;
        DamageTaken = 0f;
        KillerName = UnknownKiller;
        KillerSprite = null;
        WeaponIcon = null;
        itemIcons.Clear();
        startTime = Time.time;
        endTime = -1f;
    }
}
