using UnityEngine;

public static class DebugExtension
{
    // 빌드에서는 호출과 인자 계산까지 빠진다. 공격마다 불리므로 꼭짓점 배열을 만들지 않는다.
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DrawBox(Vector2 center, Vector2 halfSize, Quaternion rotation, Color color, float duration = 0f)
    {
        Vector2 a = (Vector3)center + rotation * new Vector3(-halfSize.x, -halfSize.y);
        Vector2 b = (Vector3)center + rotation * new Vector3( halfSize.x, -halfSize.y);
        Vector2 c = (Vector3)center + rotation * new Vector3( halfSize.x,  halfSize.y);
        Vector2 d = (Vector3)center + rotation * new Vector3(-halfSize.x,  halfSize.y);

        Debug.DrawLine(a, b, color, duration);
        Debug.DrawLine(b, c, color, duration);
        Debug.DrawLine(c, d, color, duration);
        Debug.DrawLine(d, a, color, duration);
    }
}
