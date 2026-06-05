using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/AttackData")]
public class AttackData : ScriptableObject
{
    public float duration;
    public AnimationClip animClip;
    public ContactFilter2D filter;
    public Vector2 hitboxPosition;
    public Vector2 hitboxSize;
}