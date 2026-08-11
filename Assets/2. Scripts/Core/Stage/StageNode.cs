using UnityEngine;

public enum RoomType { None, Shop, Boss, Battle }

public enum BattleRoomType { Normal, Skill, Weapon }
public enum BattleRoomTypeFilter { Any, Normal, Skill, Weapon }

public class StageNode : MonoBehaviour
{
    public RoomType roomType;
    public bool wasCleared;
}
