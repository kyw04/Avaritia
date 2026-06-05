using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StatData))]
public class StatDataEditor : Editor
{
    private StatType newKey;
    private StatType preKey;
    private StatData.StatValue newValue;
    private Dictionary<StatType, StatData.StatValue> statDictionary = new();

    public void OnEnable()
    {
        var data = (StatData)target;
        foreach (var stat in data.stats)
        {
            statDictionary.Add(stat.statType, stat.value);
        }
    }

    public void OnDisable()
    {
        SaveData();
        statDictionary.Clear();
    }

    private void SaveData()
    {
        var data = (StatData)target;
        data.stats.Clear();
        foreach (var stat in statDictionary)
        {
            data.stats.Add(new StatData.StatEntry(stat.Key, stat.Value));
        }
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var data = (StatData)target;

        EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);

        StatType removeData = StatType.None;
        foreach (var pair in statDictionary)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(pair.Key.ToString(), GUILayout.Width(150));

            switch (pair.Value)
            {
                case StatData.IntValue intValue:
                    EditorGUILayout.LabelField(intValue.value.ToString());
                    break;
                case StatData.FloatValue floatValue:
                    EditorGUILayout.LabelField(floatValue.value.ToString(CultureInfo.InvariantCulture));
                    break;
                case StatData.StringValue stringValue:
                    EditorGUILayout.LabelField(stringValue.value);
                    break;
            }

            if (GUILayout.Button("-", GUILayout.Width(30)))
            {
                removeData = pair.Key;
            }

            EditorGUILayout.EndHorizontal();
        }

        if (removeData != StatType.None)
        {
            statDictionary.Remove(removeData);
            SaveData();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Add Stat", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        Rect rect = EditorGUILayout.GetControlRect();
        Rect leftRect = new Rect(rect.x, rect.y, 150, rect.height);
        Rect rightRect = new Rect(rect.x + 155, rect.y, rect.width - 155, rect.height);

        newKey = (StatType)EditorGUI.EnumPopup(leftRect, newKey);
        if (preKey != newKey)
        {
            preKey = newKey;
            
            var type = StatParameter.GetStatParameter(newKey);
            if (type == typeof(int))
                newValue = new StatData.IntValue();
            else if (type == typeof(float))
                newValue = new StatData.FloatValue();
            else if (type == typeof(string))
                newValue = new StatData.StringValue();
            else
                newValue = null;
        }

        newValue = DrawTypeField(newValue, rightRect);
        
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Add"))
        {
            if (newKey != StatType.None)
            {
                Undo.RecordObject(data, "Add Dictionary Item");

                statDictionary[newKey] = newValue;
                SaveData();
            }
            newKey = StatType.None;
        }

        if (newKey != StatType.None && statDictionary.ContainsKey(newKey))
        {
            EditorGUILayout.HelpBox("Stat already exists", MessageType.Warning);
        }
    }
    
    private StatData.StatValue DrawTypeField(StatData.StatValue value, Rect rect)
    {
        if (value == null)
            return null;
        
        switch (value)
        {
            case StatData.IntValue intValue:
                intValue.value = EditorGUI.IntField(rect, intValue.value is var i ? i : 0);
                break;
            case StatData.FloatValue floatValue:
                floatValue.value = EditorGUI.FloatField(rect, floatValue.value is var f ? f : 0f);
                break;
            case StatData.StringValue stringValue:
                stringValue.value = EditorGUI.TextField(rect, stringValue.value is var s ? s : "");
                break;
        }
        
        return value;
    }
}
