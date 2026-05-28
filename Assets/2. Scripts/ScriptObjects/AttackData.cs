using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/AttackData")]
public class AttackData : ScriptableObject
{
    public float duration;
    public AnimationClip animClip;
    public Vector2 hitboxData;
}