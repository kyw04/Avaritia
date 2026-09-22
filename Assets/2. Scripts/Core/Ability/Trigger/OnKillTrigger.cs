using System;

[System.Serializable]
public class OnKillTrigger : IAbilityTrigger, IObserver<EntityDeadEvent>
{
    private Entity owner;
    private Action<AbilityContext> fire;

    public void Bind(Entity owner, Action<AbilityContext> fire)
    {
        this.owner = owner;
        this.fire = fire;
        EventBus.Subscribe(this);
    }

    public void Unbind(Entity owner) => EventBus.Unsubscribe(this);

    // EntityDeadEvent에는 킬러 정보가 없으므로, 실제로는 "owner가 죽인 대상"이 아니라
    // "owner 이외의 누군가가 죽었을 때" 발동한다. 킬 어트리뷰션이 필요해지면 데미지 소스 추적을 추가해야 한다.
    public void OnNotify(EntityDeadEvent e)
    {
        if (e.Source == owner) return;
        fire(new AbilityContext { Caster = owner, Target = e.Source.transform, EventSource = e.Source });
    }
}
