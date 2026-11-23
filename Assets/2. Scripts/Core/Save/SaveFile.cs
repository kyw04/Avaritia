using System;
using System.IO;
using UnityEngine;

public static class SaveFile
{
    public static string DefaultPath => Path.Combine(Application.persistentDataPath, "run_save.json");

    public static void Write(string path, RunSaveData data)
    {
        // IO 실패가 이벤트 디스패치를 끊지 않도록 예외를 삼키고 경고만 남긴다
        try
        {
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(data));
            // 임시 파일에 먼저 쓰고 교체해 쓰기 도중 손상을 막는다
            File.Copy(tmp, path, true);
            File.Delete(tmp);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            Debug.LogWarning($"SaveFile: 저장 실패 {path} - {ex.Message}");
        }
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
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            Debug.LogWarning($"SaveFile: 삭제 실패 {path} - {ex.Message}");
        }
    }
}
