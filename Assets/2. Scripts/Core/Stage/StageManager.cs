using System.Collections.Generic;
using UnityEngine;

public class StageManager : Singleton<StageManager>
{
    private static readonly List<StageNode> EmptyNodes = new();

     [SerializeField] private StageData currentStageData;
    private StageNode currentNode;
    private readonly HashSet<StageNode> clearedNodes = new();

    public StageData CurrentStageData => currentStageData;
    public StageNode CurrentNode => currentNode;
    public bool IsCurrentRoomCleared => currentNode != null && clearedNodes.Contains(currentNode);

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

        clearedNodes.Clear();
        SetCurrentNode(currentStageData.startNode);
    }

    public void NotifyRoomCleared()
    {
        if (currentNode == null || !clearedNodes.Add(currentNode))
            return;

        EventBus.Publish(new StageNodeClearedEvent(currentNode));

        // if (currentNode.nextNodes.Count == 0)
        //     EventBus.Publish(new StageCompletedEvent(CurrentStageData));
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

    public StageNode GetStage(RoomType roomType)
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
                int count = stage.battleNodes.Count;
                return stage.battleNodes[Random.Range(0, count)];
        }
        
        return null;
    }

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
        clearedNodes.Clear();
        SetCurrentNode(nextStage.startNode);
    }

    private void SetCurrentNode(StageNode node)
    {
        var previous = currentNode;
        currentNode = node;
        EventBus.Publish(new StageNodeChangedEvent(previous, currentNode));
        
        if (node.wasCleared)
            NotifyRoomCleared();
    }
}
