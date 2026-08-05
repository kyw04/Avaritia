using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Item Box Data")]
public class ItemBoxData : ScriptableObject
{
    public List<ScriptableObject> itemPool = new();
    public int itemCount = 1;
}
