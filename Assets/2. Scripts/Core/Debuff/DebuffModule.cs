using System;
using System.Collections.Generic;
using UnityEngine;

// 디버프의 동작 하나. 설정값만 갖고, 런타임 상태는 넘겨받는 ActiveDebuff에 둔다(에셋이 공유되므로).
// 효과는 ActiveDebuff를 출처 키로 걸면 끝날 때 DebuffController가 출처 단위로 걷어내므로 OnRemove는 대부분 비워 둔다.
[Serializable]
public abstract class DebuffModule
{
    // 이 크기의 적에게는 이 모듈만 무시한다. 모든 모듈이 무시되면 디버프 자체가 걸리지 않는다.
    [SerializeField] private List<EnemySize> immuneSizes = new();

    public bool Affects(Entity target) => target is not Enemy enemy || !immuneSizes.Contains(enemy.Size);

    public virtual void OnApply(ActiveDebuff debuff) { }
    // 이미 걸린 디버프를 다시 걸 때마다(중첩 수가 안 바뀌어도) 호출된다. Inflictor는 이미 새로 건 쪽이다.
    public virtual void OnReapply(ActiveDebuff debuff) { }
    public virtual void OnStacksChanged(ActiveDebuff debuff) { }
    public virtual void OnTick(ActiveDebuff debuff) { }
    public virtual void OnRemove(ActiveDebuff debuff) { }
}
