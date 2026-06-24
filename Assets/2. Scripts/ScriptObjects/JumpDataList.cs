using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/JumpDataList")]
public class JumpDataList : ScriptableObject
{
    public List<JumpData> datas = new();
}
