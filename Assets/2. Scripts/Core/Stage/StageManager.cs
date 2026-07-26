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

        currentNode = currentStageData.startNode;
        clearedNodes.Clear();
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
        if (!IsCurrentRoomCleared)
        {
            Debug.LogError($"StageManager: cannot move to {(node == null ? "null" : node.name)} from current state");
            return;
        }

        var previous = currentNode;
        currentNode = node;
        EventBus.Publish(new StageNodeChangedEvent(previous, currentNode));
    }
}
