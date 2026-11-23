using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class SaveRegistryEditor
{
    const string RegistryPath = "Assets/6. Scriptable Objects/SaveRegistry.asset";

    [MenuItem("Tools/Save/Rebuild Save Registry")]
    public static void Rebuild()
    {
        var registry = AssetDatabase.LoadAssetAtPath<SaveRegistry>(RegistryPath);
        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<SaveRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
        }

        var found = new List<ScriptableObject>();
        foreach (var type in new[] { "AbilityData", "WeaponData", "StageData" })
        {
            foreach (var guid in AssetDatabase.FindAssets("t:" + type))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null || found.Contains(asset)) continue;
                // private saveId라 SerializedObject로 GUID를 채운다.
                var so = new SerializedObject(asset);
                var prop = so.FindProperty("saveId");
                if (prop != null && prop.stringValue != guid)
                {
                    prop.stringValue = guid;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                // 이미 메모리에만 ID가 있는 에셋도 디스크에 저장되도록 항상 dirty 처리
                EditorUtility.SetDirty(asset);
                found.Add(asset);
            }
        }

        var regSo = new SerializedObject(registry);
        var list = regSo.FindProperty("assets");
        list.arraySize = found.Count;
        for (int i = 0; i < found.Count; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
        regSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();
        Debug.Log($"SaveRegistry: {found.Count}개 에셋 등록");
    }
}

// 빌드 직전에 레지스트리를 갱신해 새 에셋이 빠지지 않게 한다.
public class SaveRegistryBuildHook : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report) => SaveRegistryEditor.Rebuild();
}
