using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SaveFileTests
{
    string path;

    [SetUp]
    public void SetUp()
    {
        path = Path.Combine(Application.temporaryCachePath, "save_test.json");
        SaveFile.Delete(path);
    }

    [TearDown]
    public void TearDown()
    {
        SaveFile.Delete(path);
        SaveFile.Delete(path + ".tmp");
    }

    [Test]
    public void Write_ThenTryRead_RoundTrips()
    {
        var data = new RunSaveData
        {
            hpRatio = 0.72f,
            stage = new StageSave { nodeRole = "Battle", nodeIndex = 2, cleared = true },
            skills = new List<AssetRef> { new AssetRef { id = "", name = "" } },
            weapon = new WeaponSave
            {
                modules = new List<ModuleSave>
                {
                    new ModuleSave { index = 0, type = "MagazineModule", state = "{\"ammo\":5}" }
                }
            }
        };

        SaveFile.Write(path, data);

        Assert.IsTrue(SaveFile.TryRead(path, out var read));
        Assert.AreEqual(RunSaveData.CurrentVersion, read.version);
        Assert.AreEqual(0.72f, read.hpRatio);
        Assert.AreEqual("Battle", read.stage.nodeRole);
        Assert.AreEqual(2, read.stage.nodeIndex);
        Assert.IsTrue(read.stage.cleared);
        Assert.AreEqual(1, read.skills.Count);
        Assert.AreEqual("", read.skills[0].id);
        Assert.AreEqual("", read.skills[0].name);
        Assert.AreEqual(1, read.weapon.modules.Count);
        Assert.AreEqual(0, read.weapon.modules[0].index);
        Assert.AreEqual("MagazineModule", read.weapon.modules[0].type);
        Assert.AreEqual("{\"ammo\":5}", read.weapon.modules[0].state);
    }

    [Test]
    public void TryRead_MissingFile_ReturnsFalse()
    {
        Assert.IsFalse(SaveFile.TryRead(path, out _));
    }

    [Test]
    public void TryRead_CorruptJson_ReturnsFalse()
    {
        File.WriteAllText(path, "{not json");
        Assert.IsFalse(SaveFile.TryRead(path, out _));
    }

    [Test]
    public void TryRead_VersionMismatch_ReturnsFalse()
    {
        File.WriteAllText(path, "{\"version\":999}");
        Assert.IsFalse(SaveFile.TryRead(path, out _));
    }

    [Test]
    public void Delete_RemovesFile()
    {
        File.WriteAllText(path, "x");
        SaveFile.Delete(path);
        Assert.IsFalse(File.Exists(path));
    }

    [Test]
    public void Write_ToInvalidPath_DoesNotThrow()
    {
        string bad = Path.Combine(Application.temporaryCachePath, "no_such_dir", "save.json");
        LogAssert.Expect(LogType.Warning, new Regex("SaveFile: 저장 실패"));
        Assert.DoesNotThrow(() => SaveFile.Write(bad, new RunSaveData()));
    }

    [Test]
    public void Delete_MissingFile_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => SaveFile.Delete(path));
    }
}
