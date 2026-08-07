using System.Collections.Generic;
using UnityEngine;

public class StageManager : Singleton<StageManager>
{
    [SerializeField] private StageData currentStageData;
    private StageNode currentNode;
    private bool isCurrentNodeCleared;
    private int battleClearCount;
    private StageNode pendingSpecialNode;

    public StageData CurrentStageData => currentStageData;
    public StageNode CurrentNode => currentNode;
    public bool IsCurrentRoomCleared => currentNode != null && isCurrentNodeCleared;

    private void Start()
    {
        BeginStage();
    }

    public void BeginStage()
    {
        if (currentStageData == null)
        {
            Debug.LogError("StageManager: StageNode or its startNode is not assigned");
            return;
        }

        SetCurrentNode(currentStageData.startNode);
    }

    public void NotifyRoomCleared()
    {
        if (currentNode == null || isCurrentNodeCleared)
            return;

        isCurrentNodeCleared = true;
        EventBus.Publish(new StageNodeClearedEvent(currentNode));

        if (currentNode.roomType == RoomType.Battle)
            AdvanceBattleProgress();

        // if (currentNode.nextNodes.Count == 0)
        //     EventBus.Publish(new StageCompletedEvent(CurrentStageData));
    }

    private void AdvanceBattleProgress()
    {
        battleClearCount++;
        var stage = currentStageData;

        // boss checked first: if both distances coincide, shop overwrites (rare misconfiguration, doesn't block progression)
        if (battleClearCount == stage.bossRoomDistance - 1)
            SetPendingSpecialNode(stage.preBossNode, "preBossNode");

        if (battleClearCount == stage.shopRoomDistance - 1)
            SetPendingSpecialNode(stage.preShopNode, "preShopNode");
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
                var candidates = stage.battleNodes;
                if (battleFilter != BattleRoomTypeFilter.Any)
                {
                    var filtered = new List<StageNode>();
                    foreach (var node in stage.battleNodes)
                        if (Matches(battleFilter, node.battleRoomType))
                            filtered.Add(node);

                    if (filtered.Count > 0)
                        candidates = filtered;
                    else
                        Debug.LogWarning($"StageManager: no battle node matches filter {battleFilter}, falling back to full random pool");
                }
                return candidates[Random.Range(0, candidates.Count)];
        }

        return null;
    }

    private static bool Matches(BattleRoomTypeFilter filter, BattleRoomType type) => filter switch
    {
        BattleRoomTypeFilter.Normal => type == BattleRoomType.Normal,
        BattleRoomTypeFilter.Skill => type == BattleRoomType.Skill,
        BattleRoomTypeFilter.Weapon => type == BattleRoomType.Weapon,
        _ => false,
    };

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
        SetCurrentNode(nextStage.startNode);
    }

    private void SetCurrentNode(StageNode node)
    {
        var previous = currentNode;
        currentNode = node;
        isCurrentNodeCleared = false;
        EventBus.Publish(new StageNodeChangedEvent(previous, currentNode));

        if (node.wasCleared)
            NotifyRoomCleared();
    }
}
