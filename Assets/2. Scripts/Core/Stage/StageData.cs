using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Stage Data")]
public class StageData : ScriptableObject
{
    public StageNode startNode;
    public List<StageNode> battleNodes = new();

    public StageNode shopNode;
    public StageNode preShopNode;
    public int shopRoomDistance;

    public StageNode bossNode;
    public StageNode preBossNode;
    public int bossRoomDistance;

    public List<Weapon> weaponRewardPool = new();
    public int weaponBoxItemCount = 1;
    public List<SkillData> skillRewardPool = new();
    public int skillBoxItemCount = 1;

    public StageData nextStage;
}
