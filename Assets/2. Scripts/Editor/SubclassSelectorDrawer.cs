#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
public class SubclassSelectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            EditorGUI.LabelField(position, label.text, "Use [SubclassSelector] only with [SerializeReference]");
            return;
        }

        label = EditorGUI.BeginProperty(position, label, property);
        
        Rect typeRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
        
        string currentTypeName = property.managedReferenceFullTypename;
        string displayName = string.IsNullOrEmpty(currentTypeName) ? "Null (None)" : currentTypeName.Split('.').Last();

        if (EditorGUI.DropdownButton(typeRect, new GUIContent(displayName), FocusType.Keyboard))
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Null (None)"), string.IsNullOrEmpty(currentTypeName), () => ClearProperty(property));

            var fieldType = fieldInfo.FieldType;
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
            {
                fieldType = fieldType.GetGenericArguments()[0];
            }

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => fieldType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in types)
            {
                string typeName = type.FullName;
                menu.AddItem(new GUIContent(type.Name), currentTypeName == typeName, () => SetPropertyType(property, type));
            }
            menu.ShowAsContext();
        }

        EditorGUI.PropertyField(position, property, label, true);
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    private void SetPropertyType(SerializedProperty property, Type type)
    {
        property.managedReferenceValue = Activator.CreateInstance(type);
        property.serializedObject.ApplyModifiedProperties();
    }

    private void ClearProperty(SerializedProperty property)
    {
        property.managedReferenceValue = null;
        property.serializedObject.ApplyModifiedProperties();
    }
}
#endif
