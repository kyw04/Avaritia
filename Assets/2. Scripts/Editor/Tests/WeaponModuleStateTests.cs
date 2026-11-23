using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class WeaponModuleStateTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

    private static void SetField(object target, string name, object value) =>
        target.GetType().GetField(name, Flags).SetValue(target, value);

    private static T GetField<T>(object target, string name) =>
        (T)target.GetType().GetField(name, Flags).GetValue(target);

    private static WeaponInstance Make(WeaponModule module)
    {
        var data = ScriptableObject.CreateInstance<WeaponData>();
        data.modules.Add(module);
        return new WeaponInstance(data);
    }

    private static WeaponInstance MakeMagazine()
    {
        var module = new MagazineModule();
        SetField(module, "maxAmmo", 6);
        return Make(module);
    }

    [Test]
    public void MagazineModule_RoundTrip_PreservesAmmo()
    {
        var a = MakeMagazine();
        var b = MakeMagazine();
        SetField(a.Modules[0], "currentAmmo", 2);

        b.RestoreModuleStates(a.CaptureModuleStates());

        Assert.AreEqual(2, GetField<int>(b.Modules[0], "currentAmmo"));
        Assert.IsTrue(GetField<bool>(b.Modules[0], "initialized"));
    }

    [Test]
    public void RestoreModuleStates_TypeMismatch_Skips()
    {
        var w = MakeMagazine();
        w.RestoreModuleStates(new List<ModuleSave> { new ModuleSave { index = 0, type = "StackModule", state = "{}" } });

        Assert.AreEqual(0, GetField<int>(w.Modules[0], "currentAmmo"));
        Assert.IsFalse(GetField<bool>(w.Modules[0], "initialized"));
    }

    [Test]
    public void RestoreModuleStates_IndexOutOfRange_Skips()
    {
        var w = MakeMagazine();
        Assert.DoesNotThrow(() => w.RestoreModuleStates(
            new List<ModuleSave> { new ModuleSave { index = 5, type = "MagazineModule", state = "{\"currentAmmo\":3}" } }));
        Assert.AreEqual(0, GetField<int>(w.Modules[0], "currentAmmo"));
    }

    [Test]
    public void ForgeModule_CountsLengthMismatch_Ignored()
    {
        var module = new ForgeModule();
        SetField(module, "rewards", new List<ForgeModule.Reward> { new ForgeModule.Reward(), new ForgeModule.Reward() });
        var w = Make(module);

        w.RestoreModuleStates(new List<ModuleSave> { new ModuleSave { index = 0, type = "ForgeModule", state = "{\"counts\":[1,2,3]}" } });
        CollectionAssert.AreEqual(new[] { 0, 0 }, GetField<int[]>(w.Modules[0], "counts"));

        w.RestoreModuleStates(new List<ModuleSave> { new ModuleSave { index = 0, type = "ForgeModule", state = "{\"counts\":[4,5]}" } });
        CollectionAssert.AreEqual(new[] { 4, 5 }, GetField<int[]>(w.Modules[0], "counts"));
    }
}
