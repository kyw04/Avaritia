using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 런 이어하기. 방에 들어갈 때와 클리어한 방에서 종료할 때 저장하고, 시작할 때 복원한다.
// Player.Start(기본 무기 장착)보다 늦게 실행되어야 한다.
[DefaultExecutionOrder(100)]
public class RunSaveManager : Singleton<RunSaveManager>,
    IObserver<StageNodeChangedEvent>, IObserver<PlayerDiedEvent>, IObserver<RunResetEvent>
{
    [SerializeField] private Player player;
    [SerializeField] private SaveRegistry registry;

    private bool deleted;
    // 복원 중 플레이어를 바꾸기 시작했는지. 실패하면 플레이어만 되돌린다.
    private bool playerTouched;

    protected override void Awake()
    {
        base.Awake();
        EventBus.Subscribe<StageNodeChangedEvent>(this);
        EventBus.Subscribe<PlayerDiedEvent>(this);
        EventBus.Subscribe<RunResetEvent>(this);
    }

    private void OnDestroy() => EventBus.UnsubscribeAll(this);

    private void Start()
    {
        string path = SaveFile.DefaultPath;
        if (!File.Exists(path))
        {
            StageManager.Instance.BeginStage();
            return;
        }

        string reason = null;
        try
        {
            if (!SaveFile.TryRead(path, out var data))
                reason = "파일 손상 또는 버전 불일치";
            else if (TryRestore(data, out reason))
                return;
        }
        catch (Exception ex)
        {
            reason = ex.Message;
        }

        Debug.LogWarning($"RunSaveManager: 세이브 복원 실패, 새 런을 시작한다 - {reason}");
        SaveFile.Delete(path);
        // 다른 시스템까지 초기화되지 않게 이벤트 대신 플레이어만 되돌린다.
        if (playerTouched)
            player.OnNotify(new RunResetEvent());
        StageManager.Instance.BeginStage();
    }

    // 모든 ID를 먼저 찾고, 하나라도 없으면 아무것도 바꾸지 않는다.
    private bool TryRestore(RunSaveData d, out string reason)
    {
        var skills = new List<AbilityData>();
        foreach (var r in d.skills)
        {
            if (!TryResolve(r, out AbilityData skill, out reason)) return false;
            skills.Add(skill);
        }

        var items = new List<AbilityData>();
        foreach (var r in d.inventory)
        {
            if (!TryResolve(r, out AbilityData item, out reason)) return false;
            if (item != null) items.Add(item);
        }

        if (!TryResolve(d.weapon.asset, out WeaponData weaponData, out reason)) return false;

        if (registry.Find<StageData>(d.stage.stage?.id) == null)
        {
            reason = $"스테이지를 찾지 못함 ({d.stage.stage?.name})";
            return false;
        }

        // 모듈 상태 복원은 예외가 날 수 있으므로 플레이어를 건드리기 전에 한다.
        WeaponInstance weapon = null;
        if (weaponData != null)
        {
            weapon = new WeaponInstance(weaponData);
            weapon.RestoreModuleStates(d.weapon.modules);
        }

        // 방 입장 트리거가 스킬을 보므로 스킬을 스테이지보다 먼저 넣는다.
        playerTouched = true;
        int count = Mathf.Min(player.Abilities.SlotCount, skills.Count);
        for (int i = 0; i < count; i++)
            player.Abilities.SetAbility(i, skills[i]);

        player.Inventory.Clear();
        foreach (var item in items)
            player.Inventory.TryAdd(item);

        if (weapon != null)
            player.EquipWeapon(weapon);

        player.RestoreHealthRatio(d.hpRatio);

        if (!StageManager.Instance.TryRestoreProgress(d.stage, registry))
        {
            reason = $"스테이지 노드를 찾지 못함 ({d.stage.nodeRole} {d.stage.nodeIndex})";
            return false;
        }
        return true;
    }

    // id가 비어 있으면 빈 칸(null)으로 보고 성공한다.
    private bool TryResolve<T>(AssetRef r, out T asset, out string reason) where T : ScriptableObject, ISaveId
    {
        asset = null;
        reason = null;
        if (r == null || string.IsNullOrEmpty(r.id)) return true;
        asset = registry.Find<T>(r.id);
        if (asset != null) return true;
        reason = $"{typeof(T).Name}를 찾지 못함 ({r.name})";
        return false;
    }

    public void OnNotify(StageNodeChangedEvent e)
    {
        if (e.Restored) return;
        SaveFile.Write(SaveFile.DefaultPath, Capture(cleared: false));
    }

    public void OnNotify(PlayerDiedEvent e)
    {
        SaveFile.Delete(SaveFile.DefaultPath);
        deleted = true;
    }

    // 다시 시작: StageManager가 먼저 리셋 전 상태로 방 입장 세이브를 쓰지만,
    // 이 핸들러가 곧바로 지우므로 순서와 상관없이 디스크 결과는 올바르다.
    public void OnNotify(RunResetEvent e)
    {
        SaveFile.Delete(SaveFile.DefaultPath);
        deleted = false;
    }

    private void OnApplicationQuit()
    {
        if (StageManager.Instance.IsCurrentRoomCleared && !player.IsDead && !deleted)
            SaveFile.Write(SaveFile.DefaultPath, Capture(cleared: true));
    }

    private RunSaveData Capture(bool cleared)
    {
        var d = new RunSaveData { hpRatio = player.HealthRatio };
        for (int i = 0; i < player.Abilities.SlotCount; i++)
            d.skills.Add(SaveRegistry.ToRef(player.Abilities.AbilityAt(i)));
        foreach (var item in player.Inventory.Items)
            if (item is ISaveId s)
                d.inventory.Add(SaveRegistry.ToRef(s));
        if (player.Weapon != null)
        {
            d.weapon.asset = SaveRegistry.ToRef(player.Weapon.Data);
            d.weapon.modules = player.Weapon.CaptureModuleStates();
        }
        d.stage = StageManager.Instance.CaptureProgress();
        d.stage.cleared = cleared;
        return d;
    }
}
