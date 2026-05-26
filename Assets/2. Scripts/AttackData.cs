using System;
using System.Collections.Generic;
using UnityEngine;

public class AttackData : ScriptableObject
{
    public float duration;
    public AnimationClip animClip;
    public Vector2 hitboxData;
}

[Serializable]
public class AttackDataCombo : ScriptableObject
{
    public int Count => datas.Count;
    public List<AttackData> datas = new();
}