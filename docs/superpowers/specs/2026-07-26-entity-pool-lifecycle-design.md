# Enemy/Boss 오브젝트 풀 반납·재사용 설계

## 배경

`ObjectPoolManager`(및 `IPoolable`)는 이미 구현되어 있고 총알(`BulletMover`/`DamageDealer`)에는 정상적으로 연결되어 있다. 하지만 `RoomContentSpawner.SpawnActors()`가 몹(Enemy/Boss)을 `ObjectPoolManager.Instance.Spawn(...)`으로 꺼내 쓰는 반면, 이를 다시 풀에 반납하는 코드가 어디에도 없다:

- `EnemyDeadState.Enter()`는 `Debug.Log("죽음")` 한 줄뿐이고, `Boss.Die()`도 `StateManager.Unregister`만 할 뿐 `Despawn`을 호출하지 않는다. 죽은 몹이 씬에 영원히 남는다.
- `RoomContentSpawner.Spawn()`(방 전환)은 `aliveActors.Clear()`로 이전 방에 살아있던 몹의 추적만 지우고, 그 몹들을 반납하지 않는다.

결과적으로 몹용 풀의 큐는 항상 비어있어 매번 새로 `Instantiate`하고, 죽거나 버려진 몹은 회수되지 않고 계속 쌓인다 — 풀링이 사실상 동작하지 않는다.

이 설계는 Enemy/Boss가 죽거나(또는 방 전환으로 버려질 때) 풀에 올바르게 반납되고, 풀에서 다시 꺼내질 때 완전히 리셋된 상태로 재사용되도록 만드는 것을 다룬다.

## 범위

- `Entity`(Player 제외 하위 클래스들의 공통 리셋 로직)가 `IPoolable`을 구현해, 풀에서 꺼내질 때마다(최초 스폰 포함) 체력/스킬 쿨다운/버프/사망 플래그가 리셋되도록 한다.
- `Enemy`/`Boss`가 죽을 때 `ObjectPoolManager.Despawn()`을 호출하도록 하고, 풀에서 재사용될 때 상태머신(`Machine`)을 새로 구성해 `StateManager`에 다시 등록되도록 한다.
- `RoomContentSpawner`가 방을 이동할 때 이전 방에 남아있던 살아있는 몹도 강제로 `Despawn`한다.

**범위 밖**: 사망 연출(딜레이 후 반납), `SpawnActors`의 `entityPrefab` null 체크 부재(별개의 기존 버그), 총알 풀 경로(이미 정상 동작).

## 기존 코드베이스와의 관계

- `Entity.Awake()`가 현재 `stats`/`Skills`/`wasGroundCheckerChanged` 초기화를 담당하는데, 이는 씬에 직접 배치되어 풀을 거치지 않는 `Player`에게는 그대로 필요하다. 풀링되는 `Enemy`/`Boss`는 `Awake()`가 최초 1회만 실행되므로, 재사용 시에는 별도 리셋 경로(`OnSpawn()`)가 필요하다.
- `ObjectPoolManager.Spawn()`은 최초 `Instantiate` 시와 큐에서 재사용할 때 모두 `IPoolable.OnSpawn()`을 호출한다(코드 확인됨) — 즉 `Awake()` 직후 항상 `OnSpawn()`이 뒤따른다. 따라서 `Awake()`와 `OnSpawn()`이 같은 리셋 로직을 호출해도 최초 스폰 시 중복 실행될 뿐 부작용은 없다.
- `StateMachineBase<T>.Init()`/`BT.BehaviorTree.Init()`은 내부에서 `StateManager.Instance.Register(this)`를 호출한다. `Die()`가 `Unregister`하고 나면, 같은 `Machine` 인스턴스를 재사용해서는 다시 틱을 받을 방법이 없다 — 새 `Machine`을 만들고 `Init()`을 다시 호출해야 한다.
- `RoomContentSpawner.aliveActors`는 `EntityDeadEvent` 발행 시점에 즉시 제거되므로(`OnNotify(EntityDeadEvent e)`), 방 전환 시점에 이 리스트에 남아있는 항목은 항상 "아직 살아있는" 개체다.

## 컴포넌트

### `Entity` (기존 파일 수정)

```csharp
public abstract class Entity : MonoBehaviour, IDamageable, IAttacker, IBuffable, IPoolable
{
    // ... 기존 필드/프로퍼티 동일 ...

    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        ResetEntityState();
    }

    public virtual void OnSpawn() => ResetEntityState();

    public virtual void OnDespawn() { }

    private void ResetEntityState()
    {
        isDead = false;
        stats = new RuntimeStats(statDataAsset);
        Skills = new SkillManager(this, skill);
        activeBuffs.Clear();
        wasGroundCheckerChanged = !IsGrounded;
    }
}
```

`activeBuffs`는 현재 `private readonly List<ActiveBuff>`라 `Clear()` 호출은 클래스 내부에서만 가능 — `ResetEntityState()`가 `Entity` 내부 메서드이므로 문제없다.

### `Enemy` (기존 파일 수정)

```csharp
protected override void Awake()
{
    base.Awake();
    Owner = this;
    stateManager = StateManager.Instance;
}

public override void OnSpawn()
{
    base.OnSpawn();
    patrolCenter = transform.position;
    Target = null;
    Machine = new EnemyStateMachine(Owner);
    Machine.Init();
}

public override void Die()
{
    if (!TryMarkDead()) return;
    Machine.ChangeState<EnemyDeadState>();
    stateManager.Unregister(Machine);
    ObjectPoolManager.Instance.Despawn(gameObject);
}
```

`Owner = this`는 매 생애 동일한 값이라 `Awake()`에 남겨둔다(재대입 불필요). `patrolCenter`/`Target`/`Machine`은 스폰될 때마다(풀에서 꺼낸 새 위치 기준으로) 다시 계산해야 하므로 `OnSpawn()`으로 옮긴다.

### `Boss` (기존 파일 수정)

```csharp
protected override void Awake()
{
    base.Awake();
    stateManager = StateManager.Instance;
}

public override void OnSpawn()
{
    base.OnSpawn();
    var player = FindAnyObjectByType<Player>();
    if (player != null)
        Target = player.transform;

    Machine = new BossBehaviorTree(this);
    Machine.Init();
}

public override void Die()
{
    if (!TryMarkDead()) return;
    stateManager.Unregister(Machine);
    ObjectPoolManager.Instance.Despawn(gameObject);
    Debug.Log("Boss: 사망");
}
```

### `RoomContentSpawner` (기존 파일 수정)

```csharp
public void Spawn(StageNode node)
{
    if (currentRoom != null)
        Destroy(currentRoom);

    foreach (var actor in aliveActors)
        if (actor != null)
            ObjectPoolManager.Instance.Despawn(actor.gameObject);
    aliveActors.Clear();

    currentRoom = Instantiate(node.gameObject, Vector3.zero, Quaternion.identity);

    var stageData = StageManager.Instance.CurrentStageData;
    SpawnActors(stageData);
}
```

## 예외 처리 / 경계 조건

- `ObjectPoolManager.Despawn()`은 이미 `activeSelf == false`인 인스턴스에 대해 경고 후 무시하는 중복 방어 로직을 갖고 있다. `aliveActors`가 항상 "살아있는" 개체만 담고 있다는 전제가 깨지지 않는 한(예: `EntityDeadEvent` 처리 로직이 바뀌지 않는 한) 이 경로에서 중복 경고가 발생하지 않는다.
- `Machine`을 매 `OnSpawn()`마다 새로 생성하므로, 이전 생애의 `Machine` 인스턴스는 참조가 끊겨 GC 대상이 된다 — 별도 정리 코드가 필요 없다.
- `Boss.OnSpawn()`의 `FindAnyObjectByType<Player>()`는 기존 `Awake()`에도 있던 동일한 탐색이라 매 스폰마다 다시 수행해도 비용/동작 차이가 없다(플레이어가 씬에 하나뿐).
- `Entity.OnDespawn()`은 현재 빈 구현으로 둔다 — Enemy/Boss는 현재 정지가 필요한 코루틴이 없다(총알의 `BulletMover.OnDespawn()`처럼 `StopAllCoroutines()`가 필요한 상황이 아님). 나중에 필요해지면 하위 클래스에서 오버라이드하면 된다.
