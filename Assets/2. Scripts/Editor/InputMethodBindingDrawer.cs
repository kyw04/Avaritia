#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 입력 함수 항목을 액션 / 홀드 시간 / 단계(홀드면 다른 키 입력 시 유지 여부) / 메서드 드롭다운 / 연출(ActionFeedback)로 그린다.
// 홀드 항목은 홀드 시작 연출을 더 그린다.
// 메서드 목록은 무기의 모듈에서 [InputMethod]가 붙은 메서드를 "타입명.메서드명"으로 보여준다.
[CustomPropertyDrawer(typeof(Weapon.InputMethodBinding))]
public class InputMethodBindingDrawer : PropertyDrawer
{
    private const int LineCount = 4;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float line = EditorGUIUtility.singleLineHeight;
        float step = line + EditorGUIUtility.standardVerticalSpacing;
        var rect = new Rect(position.x, position.y, position.width, line);

        EditorGUI.PropertyField(rect, property.FindPropertyRelative("action"));
        rect.y += step;
        var holdDuration = property.FindPropertyRelative("holdDuration");
        EditorGUI.PropertyField(rect, holdDuration);
        rect.y += step;
        // 홀드는 누름/뗌으로 진행하므로 단계 대신 다른 키 입력 시 유지 여부를 보여준다.
        EditorGUI.PropertyField(rect, property.FindPropertyRelative(holdDuration.floatValue > 0f ? "keepHoldOnOtherInput" : "phase"));
        rect.y += step;
        DrawMethodPopup(rect, property);
        rect.y += step;
        var feedback = property.FindPropertyRelative("feedback");
        rect.height = EditorGUI.GetPropertyHeight(feedback, true);
        EditorGUI.PropertyField(rect, feedback, true);

        if (holdDuration.floatValue > 0f)
        {
            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
            var holdStart = property.FindPropertyRelative("holdStartFeedback");
            rect.height = EditorGUI.GetPropertyHeight(holdStart, true);
            EditorGUI.PropertyField(rect, holdStart, true);
        }

        EditorGUI.EndProperty();
    }

    private static void DrawMethodPopup(Rect rect, SerializedProperty property)
    {
        var methodName = property.FindPropertyRelative("methodName");
        var target = property.serializedObject.targetObject;
        List<string> names = target is Weapon { modules: { } modules }
            ? Weapon.GetInputMethods(modules.Where(m => m != null)).Select(m => $"{m.moduleType.Name}.{m.method.Name}").Distinct().ToList()
            : new List<string>();

        // 지정된 이름이 목록에 없으면(이름 변경/삭제) 그대로 보여 주어 잃어버리지 않게 한다.
        if (!string.IsNullOrEmpty(methodName.stringValue) && !names.Contains(methodName.stringValue))
            names.Add(methodName.stringValue + " (Not Found)");
        names.Insert(0, "(None)");

        int current = Mathf.Max(0, names.FindIndex(n => n == methodName.stringValue || n == methodName.stringValue + " (Not Found)"));
        if (string.IsNullOrEmpty(methodName.stringValue))
            current = 0;

        int selected = EditorGUI.Popup(rect, "Method", current, names.ToArray());
        if (selected != current)
            methodName.stringValue = selected == 0 ? string.Empty : names[selected];
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float height = LineCount * EditorGUIUtility.singleLineHeight + LineCount * spacing
            + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("feedback"), true);
        if (property.FindPropertyRelative("holdDuration").floatValue > 0f)
            height += spacing + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("holdStartFeedback"), true);
        return height;
    }
}
#endif
