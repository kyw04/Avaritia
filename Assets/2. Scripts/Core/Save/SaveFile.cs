using System;
using System.IO;
using UnityEngine;

public static class SaveFile
{
    public static string DefaultPath => Path.Combine(Application.persistentDataPath, "run_save.json");

    public static void Write(string path, RunSaveData data)
    {
        string tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonUtility.ToJson(data));
        // 임시 파일에 먼저 쓰고 교체해 쓰기 도중 손상을 막는다
        File.Copy(tmp, path, true);
        File.Delete(tmp);
    }

    public static bool TryRead(string path, out RunSaveData data)
    {
        data = null;
        try
        {
            if (!File.Exists(path)) return false;
            var read = JsonUtility.FromJson<RunSaveData>(File.ReadAllText(path));
            if (read == null || read.version != RunSaveData.CurrentVersion) return false;
            data = read;
            return true;
        }
        catch (Exception)
        {
            data = null;
            return false;
        }
    }

    public static void Delete(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }
}
