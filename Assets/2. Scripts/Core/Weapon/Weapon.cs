using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public abstract class Weapon : ScriptableObject, IInventoryItem
{
    public SpriteLibraryAsset spriteLibraryAsset;
    public AttackDataCombo combo;
    public bool holdToAttack;
    public Sprite icon;
    public string displayName;
    [TextArea] public string description;
    public List<AbilityData> passiveAbilities = new();
    public ActionFeedback swing;
    public ActionFeedback hit;
    public float shakeForce = 0.3f;
    [Range(0f, 1f)] public float hitStopTimeScale = 0.1f;
    [Min(0)] public float hitStopDuration = 0.08f;
    public float knockbackForce = 5f;
    public float knockbackUpForce = 2f;
    [Min(0)] public float knockbackDuration = 0.2f;

    public Entity Owner { get; private set; }
    public bool IsRuntimeInstance { get; private set; }
    public string DisplayName => displayName;
    public string Details => description;
    public Sprite Icon => icon;

    // 런타임 상태(Owner, 탄약)를 에셋에 직접 쓰지 않도록 에셋일 때만 복제한다.
    // 이미 복제본이면(떨어뜨렸다 다시 주운 무기) 그대로 반환해 복제본이 쌓이지 않게 한다.
    public Weapon GetRuntimeInstance()
    {
        if (IsRuntimeInstance)
            return this;

        var runtime = Instantiate(this);
        runtime.name = name;
        runtime.IsRuntimeInstance = true;
        return runtime;
    }

    public virtual void Equip(Entity owner, ImageCountBar countBar = null)
    {
        Owner = owner;
    }

    public virtual void Unequip() { }

    // true를 반환하면 이번 점프 입력을 무기가 쓴 것으로 보고 플레이어는 점프하지 않는다.
    public virtual bool ConsumeJump() => false;

    public virtual void Attack(IAttacker attacker, int index = 0, Transform defender = null)
    {
        EventBus.Publish(new EntityAttackStartEvent(Owner, combo.datas[index]));
        combo.datas[index].Attack(attacker, defender);
    }
}
