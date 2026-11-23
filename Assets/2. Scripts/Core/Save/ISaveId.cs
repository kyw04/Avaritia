using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 세이브 파일에서 에셋을 가리킬 때 쓰는 고유 ID
public interface ISaveId
{
    string SaveId { get; }
}

public static class SaveIdUtility
{
#if UNITY_EDITOR
    // 에셋 GUID와 saveId가 다르면(복제 등) GUID로 덮어쓴다.
    public static void Sync(ScriptableObject asset, ref string id)
    {
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long _)) return;
        if (string.IsNullOrEmpty(guid) || id == guid) return;
        id = guid;
        EditorUtility.SetDirty(asset);
    }
#endif
}
