#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

// 입력 함수 항목을 액션 / 단계 / 메서드 드롭다운 / 애니메이션 네 줄로 그린다.
// 메서드 목록은 이 에셋의 무기 타입에서 [InputMethod]가 붙은 메서드만 보여준다.
[CustomPropertyDrawer(typeof(InputWeapon.InputMethodBinding))]
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
        EditorGUI.PropertyField(rect, property.FindPropertyRelative("phase"));
        rect.y += step;
        DrawMethodPopup(rect, property);
        rect.y += step;
        EditorGUI.PropertyField(rect, property.FindPropertyRelative("clip"));

        EditorGUI.EndProperty();
    }

    private static void DrawMethodPopup(Rect rect, SerializedProperty property)
    {
        var methodName = property.FindPropertyRelative("methodName");
        var names = InputWeapon.GetInputMethods(property.serializedObject.targetObject.GetType())
            .Select(m => m.Name).Distinct().ToList();

        // 지정된 이름이 목록에 없으면(이름 변경/삭제) 그대로 보여 주어 잃어버리지 않게 한다.
        if (!string.IsNullOrEmpty(methodName.stringValue) && !names.Contains(methodName.stringValue))
            names.Add(methodName.stringValue + " (없음)");
        names.Insert(0, "(None)");

        int current = Mathf.Max(0, names.FindIndex(n => n == methodName.stringValue || n == methodName.stringValue + " (없음)"));
        if (string.IsNullOrEmpty(methodName.stringValue))
            current = 0;

        int selected = EditorGUI.Popup(rect, "Method", current, names.ToArray());
        if (selected != current)
            methodName.stringValue = selected == 0 ? string.Empty : names[selected];
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
        LineCount * EditorGUIUtility.singleLineHeight + (LineCount - 1) * EditorGUIUtility.standardVerticalSpacing;
}
#endif
