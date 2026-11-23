using System;
using System.Collections.Generic;

// 어드레서블 에셋 참조 (id 우선, 실패 시 name으로 복구)
[Serializable]
public class AssetRef
{
    public string id;
    public string name;
}

[Serializable]
public class StageSave
{
    public AssetRef stage;
    public string nodeRole;           // "Start" | "Shop" | "Boss" | "Battle" | "None"
    public int nodeIndex;
    public int battleClearCount;
    public string pendingSpecialRole; // "Start" | "Shop" | "Boss" | "Battle" | "None"
    public string battleRoomType;
    public bool cleared;
}

[Serializable]
public class ModuleSave
{
    public int index;
    public string type;
    public string state; // 모듈별 JSON 상태
}

[Serializable]
public class WeaponSave
{
    public AssetRef asset;
    public List<ModuleSave> modules = new List<ModuleSave>();
}

[Serializable]
public class RunSaveData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public StageSave stage = new StageSave();
    public float hpRatio;
    public List<AssetRef> skills = new List<AssetRef>();
    public List<AssetRef> inventory = new List<AssetRef>();
    public WeaponSave weapon = new WeaponSave();
}
