using System.Collections.Generic;
using UnityEngine;

public class StageManager : Singleton<StageManager>
{
    private static readonly List<StageNode> EmptyNodes = new();

    private StageData currentStageData;
    private StageNode currentNode;
    private readonly HashSet<StageNode> clearedNodes = new();

    public StageNode CurrentNode => currentNode;
    public bool IsCurrentRoomCleared => currentNode != null && clearedNodes.Contains(currentNode);
    public IReadOnlyList<StageNode> AvailableNextNodes => IsCurrentRoomCleared ? currentNode.nextNodes : EmptyNodes;

    public void BeginStage(StageData data)
    {
        if (data == null || data.startNode == null)
        {
            Debug.LogError("StageManager: StageData or its startNode is not assigned");
            return;
        }

        currentStageData = data;
        currentNode = data.startNode;
        clearedNodes.Clear();
    }

    public void NotifyRoomCleared()
    {
        if (currentNode == null || !clearedNodes.Add(currentNode))
            return;

        EventBus.Publish(new StageNodeClearedEvent(currentNode));

        if (currentNode.nextNodes.Count == 0)
            EventBus.Publish(new StageCompletedEvent(currentStageData));
    }

    public void MoveTo(StageNode node)
    {
        if (!IsCurrentRoomCleared || !currentNode.nextNodes.Contains(node))
        {
            Debug.LogError($"StageManager: cannot move to {(node == null ? "null" : node.name)} from current state");
            return;
        }

        var previous = currentNode;
        currentNode = node;
        EventBus.Publish(new StageNodeChangedEvent(previous, currentNode));
    }
}
