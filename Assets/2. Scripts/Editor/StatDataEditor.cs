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
        statDictionary.Clear();
        foreach (var stat in data.stats)
        {
            statDictionary.Add(stat.statType, stat.value);
        }
    }

    public void OnDisable()
    {
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
        var data = (StatData)target;

        EditorGUILayout.BeginHorizontal();
        
        GUIStyle titleStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Bold
        };
        var rect = EditorGUILayout.GetControlRect(false);
        var leftRect = new Rect(rect.x, rect.y, 150, rect.height);
        var firstLineRect = new Rect(rect.x + 145, rect.y, 1, rect.height + 2);
        var rightRect = new Rect(rect.x + 155, rect.y, rect.width - 155, rect.height);

        EditorGUI.LabelField(leftRect, "Stat", titleStyle);
        EditorGUI.DrawRect(firstLineRect, Color.gray);
        EditorGUI.LabelField(rightRect, "Value", titleStyle);
        
        EditorGUILayout.EndHorizontal();

        rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), Color.gray);
        
        StatType removeData = StatType.None;
        int rowIndex = 0;
        foreach (var pair in statDictionary)
        {
            Rect rowRect = EditorGUILayout.GetControlRect(false, 22);
            Color bgColor = (rowIndex & 1) == 1 
                ? new Color(0.27f, 0.27f, 0.27f)
                : new Color(0.20f, 0.20f, 0.20f);
            if (pair.Key == newKey)
                bgColor = Color.darkOliveGreen;

            EditorGUI.DrawRect(rowRect, bgColor);
            Rect statRect = new Rect(rowRect.x + 5, rowRect.y + 2, 140, rowRect.height);
            Rect dividerRect = new Rect(rowRect.x + 145, rowRect.y, 1, rowRect.height);
            Rect valueRect = new Rect(rowRect.x + 155, rowRect.y + 2, rowRect.width - 190, rowRect.height);
            Rect buttonRect = new Rect(rowRect.x + rowRect.width - 25, rowRect.y + 1, 20, rowRect.height - 2);

            EditorGUI.LabelField(statRect, pair.Key.ToString());
            EditorGUI.DrawRect(dividerRect, Color.gray);

            switch (pair.Value)
            {
                case StatData.IntValue intValue:
                    EditorGUI.LabelField(valueRect, intValue.value.ToString());
                    break;

                case StatData.FloatValue floatValue:
                    EditorGUI.LabelField(
                        valueRect,
                        floatValue.value.ToString(CultureInfo.InvariantCulture));
                    break;

                case StatData.StringValue stringValue:
                    EditorGUI.LabelField(valueRect, stringValue.value);
                    break;
            }

            if (GUI.Button(buttonRect, "-"))
            {
                removeData = pair.Key;
            }

            rowIndex++;
        }

        if (removeData != StatType.None)
        {
            statDictionary.Remove(removeData);
            SaveData();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Add Stat", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        rect = EditorGUILayout.GetControlRect();
        leftRect = new Rect(rect.x, rect.y, 150, rect.height);
        rightRect = new Rect(rect.x + 155, rect.y, rect.width - 155, rect.height);

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

        EditorGUILayout.Space(5);
        if (GUILayout.Button("Add"))
        {
            if (newKey != StatType.None)
            {
                statDictionary[newKey] = newValue;
                SaveData();
            }
            
            newKey = StatType.None;
            newValue = null;
            preKey = StatType.None;
        }

        if (newKey != StatType.None && statDictionary.ContainsKey(newKey))
        {
            EditorGUILayout.HelpBox(
                "Stat already exists.\n" +
                       "If you add, the value is overwritten.", 
                MessageType.Warning);
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
