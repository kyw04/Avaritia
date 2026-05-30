using UnityEngine;

[CreateAssetMenu(fileName = "patrol_area", menuName = "Scriptable Objects/PatrolArea")]
public class PatrolArea : ScriptableObject
{
    public Vector2 position;
    public Vector2 size;
}