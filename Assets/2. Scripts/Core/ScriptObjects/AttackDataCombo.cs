using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/AttackDataCombo")]
public class AttackDataCombo : ScriptableObject
{
    public int Count => datas.Count;
    public List<AttackData> datas = new();
}