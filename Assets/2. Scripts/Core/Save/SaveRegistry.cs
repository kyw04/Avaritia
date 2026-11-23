using System.Collections.Generic;
using UnityEngine;

// 세이브 ID로 에셋을 찾기 위한 목록 (Tools/Save/Rebuild Save Registry로 채운다)
public class SaveRegistry : ScriptableObject
{
    [SerializeField] private List<ScriptableObject> assets = new();

    private Dictionary<string, ScriptableObject> map;

    public T Find<T>(string id) where T : ScriptableObject, ISaveId
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (map == null)
        {
            map = new Dictionary<string, ScriptableObject>();
            foreach (var asset in assets)
                if (asset is ISaveId s && !string.IsNullOrEmpty(s.SaveId))
                    map[s.SaveId] = asset;
        }
        return map.TryGetValue(id, out var found) ? found as T : null;
    }

    public static AssetRef ToRef(ISaveId asset)
    {
        if (asset == null || (asset is Object o && o == null))
            return new AssetRef { id = "", name = "" };
        return new AssetRef { id = asset.SaveId, name = ((Object)asset).name };
    }
}
