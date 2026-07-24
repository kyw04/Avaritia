using System.Collections.Generic;
using UnityEngine;

public enum RoomType { Battle, Shop, Boss }

[CreateAssetMenu(menuName = "Scriptable Objects/Stage Node")]
public class StageNode : ScriptableObject
{
    public RoomType roomType;
    public List<StageNode> nextNodes = new();
}
