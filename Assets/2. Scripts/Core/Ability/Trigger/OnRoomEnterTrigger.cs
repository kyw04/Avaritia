using System;

// 새 방(스테이지 노드)에 들어갈 때마다 발동. fireOnBind면 장착(바인딩) 즉시 한 번 더 발동해
// "장착하자마자 + 방마다 충전" 류 아이템이 다음 방까지 비어있지 않게 한다.
[Serializable]
public class OnRoomEnterTrigger : IAbilityTrigger, IObserver<StageNodeChangedEvent>
{
    public bool fireOnBind = true;
    private Entity owner;
    private Action<AbilityContext> fire;

    public void Bind(Entity owner, AbilityData data, Action<AbilityContext> fire)
    {
        this.owner = owner;
        this.fire = fire;
        EventBus.Subscribe(this);
        if (fireOnBind) Fire();
    }

    public void Unbind(Entity owner) => EventBus.Unsubscribe(this);

    public void OnNotify(StageNodeChangedEvent e) => Fire();

    private void Fire() => fire(new AbilityContext { Caster = owner, Target = owner.transform });
}
