using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class SaveRegistryTests
{
    const string Folder = "Assets/__SaveTestTemp";

    static readonly FieldInfo SaveIdField =
        typeof(WeaponData).GetField("saveId", BindingFlags.Instance | BindingFlags.NonPublic);

    static readonly FieldInfo AssetsField =
        typeof(SaveRegistry).GetField("assets", BindingFlags.Instance | BindingFlags.NonPublic);

    [TearDown]
    public void TearDown()
    {
        if (AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.DeleteAsset(Folder);
    }

    [Test]
    public void Sync_DuplicatedAsset_GetsOwnGuid()
    {
        AssetDatabase.CreateFolder("Assets", "__SaveTestTemp");
        var pathA = Folder + "/A.asset";
        var pathB = Folder + "/B.asset";
        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<WeaponData>(), pathA);
        var a = AssetDatabase.LoadAssetAtPath<WeaponData>(pathA);
        // 생성 직후 OnValidate가 돌지 않았을 수 있어 직접 호출한다.
        InvokeOnValidate(a);
        AssetDatabase.CopyAsset(pathA, pathB);
        var b = AssetDatabase.LoadAssetAtPath<WeaponData>(pathB);

        // 복제본은 A의 saveId를 그대로 가진 상태
        SaveIdField.SetValue(b, a.SaveId);
        InvokeOnValidate(b);

        Assert.AreEqual(AssetDatabase.AssetPathToGUID(pathB), b.SaveId);
        Assert.AreNotEqual(a.SaveId, b.SaveId);
    }

    [Test]
    public void Find_KnownId_ReturnsAsset()
    {
        var (registry, a) = MakeRegistry("id-a");
        Assert.AreSame(a, registry.Find<WeaponData>("id-a"));
    }

    [Test]
    public void Find_UnknownId_ReturnsNull()
    {
        var (registry, _) = MakeRegistry("id-a");
        Assert.IsNull(registry.Find<WeaponData>("nope"));
    }

    [Test]
    public void Find_EmptyId_ReturnsNull()
    {
        var (registry, _) = MakeRegistry("id-a");
        Assert.IsNull(registry.Find<WeaponData>(""));
        Assert.IsNull(registry.Find<WeaponData>(null));
    }

    static (SaveRegistry, WeaponData) MakeRegistry(string id)
    {
        var a = ScriptableObject.CreateInstance<WeaponData>();
        SaveIdField.SetValue(a, id);
        var registry = ScriptableObject.CreateInstance<SaveRegistry>();
        AssetsField.SetValue(registry, new System.Collections.Generic.List<ScriptableObject> { a });
        return (registry, a);
    }

    static void InvokeOnValidate(ScriptableObject so) =>
        so.GetType().GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Invoke(so, null);
}
