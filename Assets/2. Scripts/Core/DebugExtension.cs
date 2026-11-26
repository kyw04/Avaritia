using UnityEngine;

public static class DebugExtension
{
    // 빌드에서는 호출과 인자 계산까지 빠진다.
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DrawBox(Vector2 center, Vector2 halfSize, Quaternion rotation, Color color, float duration = 0f)
    {
        Vector3[] localPoints =
        {
            new(-halfSize.x, -halfSize.y),
            new( halfSize.x, -halfSize.y),
            new( halfSize.x,  halfSize.y),
            new(-halfSize.x,  halfSize.y),
        };

        Vector2[] worldPoints = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            worldPoints[i] = (Vector3)center + (rotation * localPoints[i]);
        }
        
        Debug.DrawLine(worldPoints[0], worldPoints[1], color, duration);
        Debug.DrawLine(worldPoints[1], worldPoints[2], color, duration);
        Debug.DrawLine(worldPoints[2], worldPoints[3], color, duration);
        Debug.DrawLine(worldPoints[3], worldPoints[0], color, duration);
    }
}