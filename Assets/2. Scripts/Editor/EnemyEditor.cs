using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Enemy))]
public class EnemyEditor : Editor
{
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        Enemy enemy = (Enemy)target;
        if (enemy == null) return;

        SerializedProperty sizeProp = serializedObject.FindProperty("patrolSize");
        Vector2 size = sizeProp.vector2Value;
        Vector3 center = enemy.transform.position;
        float handleSize = HandleUtility.GetHandleSize(center) * 0.8f;

        EditorGUI.BeginChangeCheck();
        Vector3 newSize = Handles.ScaleHandle(new Vector3(size.x, size.y, 0f), center, Quaternion.identity, handleSize);
        if (EditorGUI.EndChangeCheck())
        {
            newSize.x = Mathf.Max(0f, newSize.x);
            newSize.y = Mathf.Max(0f, newSize.y);
            size = new Vector2(newSize.x, newSize.y);
            sizeProp.vector2Value = size;
            serializedObject.ApplyModifiedProperties();
        }

        Handles.color = Color.green;
        Handles.DrawWireCube(center, new Vector3(size.x, size.y, 0f));

        sceneView.Repaint();
    }
}
