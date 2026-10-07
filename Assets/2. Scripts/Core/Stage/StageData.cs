using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Stage Data")]
public class StageData : ScriptableObject
{
    public StageNode startNode;
    public List<StageNode> battleNodes = new();

    public StageNode shopNode;
    public int shopRoomDistance;

    public StageNode bossNode;
    public int bossRoomDistance;

    public RewardPool weaponRewardPool;
    public RewardPool skillRewardPool;
    public RewardPool itemRewardPool;

    public StageData nextStage;
}
