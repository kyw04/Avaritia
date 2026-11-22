using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AttackData))]
public class AttackDataEditor : Editor
{
    private float scale = 1f;
    
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
        serializedObject.Update();
        scale = EditorGUILayout.FloatField("Inspector Sprite Scale", scale);

        SerializedProperty canTurnProp = serializedObject.FindProperty("canTurn");
        SerializedProperty canMoveProp = serializedObject.FindProperty("canMove");

        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            if ((iterator.propertyPath == "canMove" || iterator.propertyPath == "moveDuration" || iterator.propertyPath == "moveForce")
                && !canTurnProp.boolValue)
                continue;

            using (new EditorGUI.DisabledScope(iterator.propertyPath == "m_Script"))
            {
                EditorGUILayout.PropertyField(iterator, true);
            }

            if (iterator.propertyPath == "canTurn" && !canTurnProp.boolValue)
                canMoveProp.boolValue = false;
        }

        // 프리팹을 넣으면 애니메이션이 그리는 스프라이트(Animator가 붙은 자식)의 루트 기준 위치를 가져온다.
        var prefab = (GameObject)EditorGUILayout.ObjectField("Get Offset From Prefab", null, typeof(GameObject), false);
        if (prefab != null)
        {
            var animator = prefab.GetComponentInChildren<Animator>(true);
            if (animator != null)
                serializedObject.FindProperty("previewSpriteOffset").vector2Value =
                    prefab.transform.InverseTransformPoint(animator.transform.position);
            else
                Debug.LogWarning($"{prefab.name}: Animator가 없어 스프라이트 위치를 가져올 수 없습니다.");
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void CustomOnSceneGUI(SceneView sceneView)
    {
        AttackData data = (AttackData)target;
        if (data == null) return;

        DrawSpriteInScene(data);

        EditorGUI.BeginChangeCheck();

        if (data.attackStrategy is MeleeAttackStrategy strategy)
        {
            Vector3 worldCenter = Handles.PositionHandle(strategy.hitboxPosition, Quaternion.identity);
            float handleSize = HandleUtility.GetHandleSize(worldCenter) * 0.8f;
            Vector3 newSize = Handles.ScaleHandle(strategy.hitboxSize, worldCenter, Quaternion.identity, handleSize);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(data, "Modify Box Bounds");

                strategy.hitboxPosition = worldCenter;
                newSize.x = Mathf.Max(0.1f, newSize.x);
                newSize.y = Mathf.Max(0.1f, newSize.y);
                newSize.z = Mathf.Max(0.1f, newSize.z);
                strategy.hitboxSize = newSize;

                EditorUtility.SetDirty(data);
            }

            Matrix4x4 cubeTransform = Matrix4x4.TRS(worldCenter, Quaternion.identity, Vector3.one);
            using (new Handles.DrawingScope(cubeTransform))
            {
                Handles.color = Color.green;
                Handles.DrawWireCube(Vector3.zero, strategy.hitboxSize);
            }

            sceneView.Repaint();
        }
        else if (data.attackStrategy is RangedAttackStrategy rangedStrategy)
        {
            // 오른쪽을 바라보는 기준으로 총구 위치를 드래그해 정한다.
            Vector3 muzzle = Handles.PositionHandle(rangedStrategy.muzzleOffset, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(data, "Modify Muzzle Offset");
                rangedStrategy.muzzleOffset = muzzle;
                EditorUtility.SetDirty(data);
            }

            DrawRangedAttackGizmo(rangedStrategy);
            sceneView.Repaint();
        }
    }

    private void DrawRangedAttackGizmo(RangedAttackStrategy strategy)
    {
        int count = Mathf.Max(1, strategy.bulletCount);
        Vector3 muzzle = strategy.muzzleOffset;

        Handles.color = Color.cyan;
        for (int i = 0; i < count; i++)
        {
            float angle = count > 1
                ? Mathf.Lerp(-strategy.spreadAngle / 2f, strategy.spreadAngle / 2f, (float)i / (count - 1))
                : 0f;
            Vector3 dir = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
            Handles.DrawLine(muzzle, muzzle + dir);
        }
    }

    private void DrawSpriteInScene(AttackData data)
    {
        var clip = data.attack.animClip;
        if (clip == null) return;

        float animTime = (float)(EditorApplication.timeSinceStartup % clip.length);
        Sprite sprite = GetSpriteFromClip(clip, animTime);
        if (sprite == null || sprite.texture == null) return;

        float ppu = sprite.pixelsPerUnit;

        Vector2 worldSize = new Vector2(
            sprite.rect.width  / ppu,
            sprite.rect.height / ppu) * scale;

        Vector2 pivotWorld = (sprite.pivot / ppu) * scale;

        Vector3 origin = data.previewSpriteOffset;
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