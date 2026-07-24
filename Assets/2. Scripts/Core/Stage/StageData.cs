using System.Collections.Generic;
using UnityEngine;

public class StageData : MonoBehaviour
{
    public StageNode startNode;
    public List<GameObject> battleRoomPrefabs = new();
    public GameObject bossRoomPrefab;
    public GameObject shopRoomPrefab;
    public GameObject enemyPrefab;
    public GameObject bossPrefab;
}
