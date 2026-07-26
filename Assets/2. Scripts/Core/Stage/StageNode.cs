using UnityEngine;

public enum RoomType { None, Shop, Boss, Battle }

public class StageNode : MonoBehaviour
{
    public RoomType roomType;
}