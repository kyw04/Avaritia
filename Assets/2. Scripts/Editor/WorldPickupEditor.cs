using UnityEditor;
using UnityEngine;

// 픽업을 선택하거나 인스펙터에서 무기/어빌리티를 지정하면 씬에서도 바로 보이도록 스프라이트를 그 아이콘으로 바꾼다.
// 우선순위는 런타임(WorldPickup.Awake)과 같게 무기가 먼저다.
[CustomEditor(typeof(WorldPickup))]
public class WorldPickupEditor : Editor
{
    private void OnEnable() => ApplyIcon();

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
            ApplyIcon();
    }

    private void ApplyIcon()
    {
        var weapon = serializedObject.FindProperty("weaponAsset").objectReferenceValue as Weapon;
        var ability = serializedObject.FindProperty("abilityAsset").objectReferenceValue as AbilityData;
        var spriteRenderer = serializedObject.FindProperty("spriteRenderer").objectReferenceValue as SpriteRenderer;
        if (spriteRenderer == null) return;

        Sprite icon = weapon != null ? weapon.icon : ability != null ? ability.icon : null;
        if (icon == null || spriteRenderer.sprite == icon) return;

        Undo.RecordObject(spriteRenderer, "Change Pickup Sprite");
        spriteRenderer.sprite = icon;
    }
}
