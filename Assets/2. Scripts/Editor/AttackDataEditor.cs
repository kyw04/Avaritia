using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AttackData))]
public class AttackDataEditor : Editor
{
    private float scale = 2f;
    
    private void OnEnable()
    {
        SceneView.duringSceneGui += CustomOnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= CustomOnSceneGUI;
    }

    public override bool HasPreviewGUI() => false;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }

    private void CustomOnSceneGUI(SceneView sceneView)
    {
        AttackData data = (AttackData)target;
        if (data == null) return;

        DrawSpriteInScene(data);

        EditorGUI.BeginChangeCheck();

        Vector3 worldCenter = Handles.PositionHandle(data.hitboxPosition, Quaternion.identity);
        float handleSize = HandleUtility.GetHandleSize(worldCenter) * 0.8f;
        Vector3 newSize = Handles.ScaleHandle(data.hitboxSize, worldCenter, Quaternion.identity, handleSize);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(data, "Modify Box Bounds");

            data.hitboxPosition = worldCenter;
            newSize.x = Mathf.Max(0.1f, newSize.x);
            newSize.y = Mathf.Max(0.1f, newSize.y);
            newSize.z = Mathf.Max(0.1f, newSize.z);
            data.hitboxSize = newSize;

            EditorUtility.SetDirty(data);
        }

        Matrix4x4 cubeTransform = Matrix4x4.TRS(worldCenter, Quaternion.identity, Vector3.one);
        using (new Handles.DrawingScope(cubeTransform))
        {
            Handles.color = Color.green;
            Handles.DrawWireCube(Vector3.zero, data.hitboxSize);
        }

        sceneView.Repaint();
    }

    private void DrawSpriteInScene(AttackData data)
    {
        if (data.animClip == null) return;

        float animTime = (float)(EditorApplication.timeSinceStartup % data.animClip.length);
        Sprite sprite = GetSpriteFromClip(data.animClip, animTime);
        if (sprite == null || sprite.texture == null) return;

        float ppu = sprite.pixelsPerUnit;

        Vector2 worldSize = new Vector2(
            sprite.rect.width  / ppu,
            sprite.rect.height / ppu) * scale;

        Vector2 pivotWorld = (sprite.pivot / ppu) * scale;

        Vector3 origin = Vector3.zero;
        Vector3 bottomLeft = origin - (Vector3)pivotWorld;
        Vector3 topRight   = bottomLeft + new Vector3(worldSize.x, worldSize.y, 0);

        Texture2D tex = sprite.texture;
        Rect tr = sprite.textureRect;
        Rect uv = new Rect(
            tr.x / tex.width,
            tr.y / tex.height,
            tr.width / tex.width,
            tr.height / tex.height);

        Vector2 sBL = HandleUtility.WorldToGUIPoint(bottomLeft);
        Vector2 sTR = HandleUtility.WorldToGUIPoint(topRight);

        Rect screenRect = new Rect(
            sBL.x,
            sTR.y,
            sTR.x - sBL.x,
            sBL.y - sTR.y);

        Handles.BeginGUI();
        GUI.DrawTextureWithTexCoords(screenRect, tex, uv, true);
        Handles.EndGUI();
    }

    private Sprite GetSpriteFromClip(AnimationClip clip, float time)
    {
        var editorCurves = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        foreach (var binding in editorCurves)
        {
            if (binding.propertyName == "m_Sprite")
            {
                var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                if (keyframes != null && keyframes.Length > 0)
                {
                    ObjectReferenceKeyframe selectedKey = keyframes[0];
                    for (int i = 0; i < keyframes.Length; i++)
                    {
                        if (keyframes[i].time <= time)
                            selectedKey = keyframes[i];
                        else
                            break;
                    }

                    return selectedKey.value as Sprite;
                }
            }
        }

        return null;
    }
}