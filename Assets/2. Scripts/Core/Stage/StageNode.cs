using UnityEngine;

public enum RoomType { None, Shop, Boss, Battle }

public enum BattleRoomType { Normal, Skill, Weapon, Item }
public enum BattleRoomTypeFilter { Any, Normal, Skill, Weapon, Item }

public class StageNode : MonoBehaviour
{
    public RoomType roomType;
    public bool wasCleared;
}
