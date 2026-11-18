using UnityEditor;
using UnityEngine;

// 씬 뷰에서 스폰 포인트의 순찰 범위 위치(이동 핸들)와 크기(크기 핸들)를 조절한다.
[CustomEditor(typeof(EntitySpawnPoint))]
public class EntitySpawnPointEditor : Editor
{
    private void OnSceneGUI()
    {
        var spawnPoint = (EntitySpawnPoint)target;
        SerializedProperty offsetProp = serializedObject.FindProperty("patrolOffset");
        SerializedProperty sizeProp = serializedObject.FindProperty("patrolSize");
        Vector2 size = sizeProp.vector2Value;
        Vector3 origin = spawnPoint.transform.position;
        Vector3 center = spawnPoint.PatrolCenter;
        float handleSize = HandleUtility.GetHandleSize(center) * 0.8f;

        EditorGUI.BeginChangeCheck();
        Vector3 newCenter = Handles.PositionHandle(center, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            offsetProp.vector2Value = (Vector2)(newCenter - origin);
            serializedObject.ApplyModifiedProperties();
            center = spawnPoint.PatrolCenter;
        }

        EditorGUI.BeginChangeCheck();
        Vector3 newSize = Handles.ScaleHandle(new Vector3(size.x, size.y, 0f), center, Quaternion.identity, handleSize);
        if (EditorGUI.EndChangeCheck())
        {
            size = new Vector2(Mathf.Max(0f, newSize.x), Mathf.Max(0f, newSize.y));
            sizeProp.vector2Value = size;
            serializedObject.ApplyModifiedProperties();
        }

        Handles.color = Color.green;
        Handles.DrawWireCube(center, new Vector3(size.x, size.y, 0f));
        Handles.DrawDottedLine(origin, center, 4f);
    }
}
