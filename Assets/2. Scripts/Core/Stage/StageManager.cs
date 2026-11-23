using System.Collections.Generic;
using UnityEngine;

public class StageManager : Singleton<StageManager>, IObserver<RunResetEvent>
{
    [SerializeField] private StageData currentStageData;
    private StageData initialStageData;
    private StageNode currentNode;
    private bool isCurrentNodeCleared;
    private int battleClearCount;
    private StageNode pendingSpecialNode;

    public StageData CurrentStageData => currentStageData;
    public StageNode CurrentNode => currentNode;
    public bool IsCurrentRoomCleared => currentNode != null && isCurrentNodeCleared;
    public bool HasPendingSpecialNode => pendingSpecialNode != null;
    public BattleRoomType CurrentBattleRoomType { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        initialStageData = currentStageData;
        EventBus.Subscribe<RunResetEvent>(this);
    }

    private void OnDestroy() => EventBus.UnsubscribeAll(this);

    // 다시 시작: 남은 풀 오브젝트를 먼저 걷어낸 뒤 첫 스테이지 시작 방부터 다시 만든다.
    // 회수를 방 생성보다 먼저 해야 새로 스폰한 적까지 걷어내지 않는다.
    public void OnNotify(RunResetEvent e)
    {
        ObjectPoolManager.Instance.DespawnAll();
        currentStageData = initialStageData;
        BeginStage();
    }

    public void BeginStage()
    {
        if (currentStageData == null)
        {
            Debug.LogError("StageManager: StageNode or its startNode is not assigned");
            return;
        }

        battleClearCount = 0;
        pendingSpecialNode = null;
        SetCurrentNode(currentStageData.startNode);
    }

    public void NotifyRoomCleared() => NotifyRoomCleared(false);

    // restored: 세이브 복원. 진행도는 저장값에 이미 반영되어 있으므로 다시 올리지 않는다.
    private void NotifyRoomCleared(bool restored)
    {
        if (currentNode == null || isCurrentNodeCleared)
            return;

        isCurrentNodeCleared = true;

        if (!restored && currentNode.roomType == RoomType.Battle)
            AdvanceBattleProgress();

        EventBus.Publish(new StageNodeClearedEvent(currentNode, restored));

        // if (currentNode.nextNodes.Count == 0)
        //     EventBus.Publish(new StageCompletedEvent(CurrentStageData));
    }

    private void AdvanceBattleProgress()
    {
        battleClearCount++;
        var stage = currentStageData;

        // boss checked first: if both distances coincide, shop overwrites (rare misconfiguration, doesn't block progression)
        if (battleClearCount == stage.bossRoomDistance - 1)
            SetPendingSpecialNode(stage.bossNode, "bossNode");

        if (battleClearCount == stage.shopRoomDistance - 1)
            SetPendingSpecialNode(stage.shopNode, "shopNode");
    }

    private void SetPendingSpecialNode(StageNode node, string fieldName)
    {
        if (node == null)
        {
            Debug.LogWarning($"StageManager: {fieldName} is not assigned, skipping special node reveal");
            return;
        }
        pendingSpecialNode = node;
    }

    public void MoveTo(StageNode node)
    {
        if (node == null || !IsCurrentRoomCleared)
        {
            Debug.LogError($"StageManager: cannot move to {(node == null ? "null" : node.name)} - current room not cleared");
            return;
        }
        SetCurrentNode(node);
    }

    // For RoomType.Battle, consumes and clears pendingSpecialNode if one is set — calling this twice can yield different results.
    // Also sets CurrentBattleRoomType as a side effect for RoomType.Battle — this is the only point that decides "what type is this visit's room".
    public StageNode GetStage(RoomType roomType, BattleRoomTypeFilter battleFilter = BattleRoomTypeFilter.Any)
    {
        var stage = currentStageData;
        switch (roomType)
        {
            case RoomType.None:
                return stage.startNode;
            case RoomType.Shop:
                return stage.shopNode;
            case RoomType.Boss:
                return stage.bossNode;
            case RoomType.Battle:
                if (pendingSpecialNode != null)
                {
                    var special = pendingSpecialNode;
                    pendingSpecialNode = null;
                    return special;
                }

                CurrentBattleRoomType = ResolveBattleRoomType(battleFilter);
                var candidates = stage.battleNodes;
                return candidates[Random.Range(0, candidates.Count)];
        }

        return null;
    }

    private static BattleRoomType ResolveBattleRoomType(BattleRoomTypeFilter filter) => filter switch
    {
        BattleRoomTypeFilter.Normal => BattleRoomType.Normal,
        BattleRoomTypeFilter.Skill => BattleRoomType.Skill,
        BattleRoomTypeFilter.Weapon => BattleRoomType.Weapon,
        BattleRoomTypeFilter.Item => BattleRoomType.Item,
        _ => RandomRewardRoomTypes[Random.Range(0, RandomRewardRoomTypes.Length)],
    };

    // 문 필터가 Any일 때 고르는 방 종류. 보상이 없는 Normal은 무작위로 나오지 않는다.
    private static readonly BattleRoomType[] RandomRewardRoomTypes =
        { BattleRoomType.Skill, BattleRoomType.Weapon, BattleRoomType.Item };

    public void AdvanceStage(StageData nextStage)
    {
        if (!IsCurrentRoomCleared)
        {
            Debug.LogError("StageManager: cannot advance stage - current room not cleared");
            return;
        }

        if (nextStage == null)
        {
            Debug.LogError("StageManager: nextStage is null");
            return;
        }

        currentStageData = nextStage;
        battleClearCount = 0;
        pendingSpecialNode = null;
        SetCurrentNode(nextStage.startNode);
    }

    private void SetCurrentNode(StageNode node, bool restoredCleared = false)
    {
        var previous = currentNode;
        currentNode = node;
        isCurrentNodeCleared = false;
        EventBus.Publish(new StageNodeChangedEvent(previous, currentNode, restoredCleared));

        if (restoredCleared)
            NotifyRoomCleared(true);
        else if (node.wasCleared)
            NotifyRoomCleared();
    }

    public StageSave CaptureProgress()
    {
        var stage = currentStageData;
        var (role, index) = NodeToRole(stage, currentNode);
        return new StageSave
        {
            stage = SaveRegistry.ToRef(stage),
            nodeRole = role,
            nodeIndex = index,
            battleClearCount = battleClearCount,
            pendingSpecialRole = NodeToRole(stage, pendingSpecialNode).role,
            battleRoomType = CurrentBattleRoomType.ToString(),
            cleared = isCurrentNodeCleared,
        };
    }

    // 스테이지나 노드를 찾지 못하면 아무것도 바꾸지 않고 false를 반환한다.
    public bool TryRestoreProgress(StageSave save, SaveRegistry registry)
    {
        var stage = registry.Find<StageData>(save.stage.id);
        if (stage == null)
            return false;

        var node = RoleToNode(stage, save.nodeRole, save.nodeIndex);
        if (node == null)
            return false;

        currentStageData = stage;
        battleClearCount = save.battleClearCount;
        pendingSpecialNode = RoleToNode(stage, save.pendingSpecialRole, 0);
        CurrentBattleRoomType = System.Enum.TryParse(save.battleRoomType, out BattleRoomType type) ? type : BattleRoomType.Normal;
        SetCurrentNode(node, save.cleared);
        return true;
    }

    // 같은 프리팹이 여러 역할이면 Start, Shop, Boss, Battle 순으로 앞의 것을 택한다.
    private static (string role, int index) NodeToRole(StageData stage, StageNode node)
    {
        if (stage == null || node == null) return ("None", 0);
        if (node == stage.startNode) return ("Start", 0);
        if (node == stage.shopNode) return ("Shop", 0);
        if (node == stage.bossNode) return ("Boss", 0);
        int i = stage.battleNodes.IndexOf(node);
        return i >= 0 ? ("Battle", i) : ("None", 0);
    }

    private static StageNode RoleToNode(StageData stage, string role, int index) => role switch
    {
        "Start" => stage.startNode,
        "Shop" => stage.shopNode,
        "Boss" => stage.bossNode,
        "Battle" => index >= 0 && index < stage.battleNodes.Count ? stage.battleNodes[index] : null,
        _ => null,
    };
}
