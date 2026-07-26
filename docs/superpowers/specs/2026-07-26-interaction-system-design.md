# 픽업 시스템의 상호작용(Interaction) 시스템으로의 일반화 설계

## 배경

[기존 픽업 설계](2026-07-15-pickup-interaction-design.md)에서 이미 "IInteractable/Pickup 류의 상호작용 시스템이 프로젝트에 전혀 없다"고 짚었지만, 당시엔 픽업(무기/스킬)만 범위였다. 이제 문/레버 같은 기믹, NPC 대화/상점 등 픽업이 아닌 상호작용 대상이 추가될 예정이라, `IPickupable` 중심의 현재 구조를 일반화해야 한다.

현재 구현(`IPickupable`, `WorldPickup`, `WorldPickupManager`, `PlayerPickupController`, `PickupPromptUI`)은 실제 코드가 원 설계 문서와 한 가지 차이가 있다: 프롬프트가 `WorldPickup`마다 딸린 자식이 아니라 `PlayerPickupController`가 들고 있는 공유 UI 하나이며, 현재 감지 대상의 위치로 따라다닌다. 이 덕분에 일반화 과정에서 프롬프트 관련 변경은 필요 없다.

## 범위

- `IPickupable` → `IInteractable`로 인터페이스를 일반화하고, 감지/입력/프롬프트 파이프라인이 특정 타입(`WorldPickup`)이 아닌 인터페이스에 의존하도록 변경
- 상호작용 후 "대상을 파괴할지"를 컨트롤러가 아니라 각 인터랙터블 구현체가 스스로 결정하도록 책임 이동 (픽업은 파괴, 문 같은 대상은 유지)
- 기존 픽업 기능(무기/스킬 획득, 탭/홀드 제스처, 교체 시 드롭)은 동작 변화 없이 그대로 유지
- 관련 클래스/파일 이름을 새 역할에 맞게 정리

**범위 밖**: 문/레버/NPC 대화 등 구체적인 새 상호작용 대상의 실제 구현. 이번 설계는 그런 것들이 `IInteractable` 구현체 하나 추가만으로 편입될 수 있는 프레임워크를 만드는 데까지만 다룬다.

## 기존 코드베이스와의 관계

- `WorldPickup.Awake()`/`OnDestroy()`가 이미 매니저에 자가 등록/해제하는 패턴을 쓰고 있음 — 이 패턴을 그대로 "각 인터랙터블이 스스로 등록"하는 일반 규칙으로 채택한다. 공통 베이스 MonoBehaviour를 강제하지 않는다(문이 나중에 다른 베이스 클래스를 상속해야 할 수도 있으므로).
- `PlayerPickupController.Resolve()`가 현재 대상을 직접 `Destroy()`하는데, 이는 픽업에만 맞는 규칙이라 일반 컨트롤러에서 제거하고 `WorldPickup.Interact()` 자신이 처리하도록 옮긴다.
- `WeaponPickup`/`SkillPickup`의 `Pickup(player, choice, dropPosition)`에서 `dropPosition`은 호출부에서 항상 `player.transform.position`이었으므로, 인터페이스에서 파라미터를 제거하고 구현체 내부에서 직접 참조한다.
- `InputHandler`의 `Interact` 액션 구독과 `Player.PickupController` 참조는 이름만 바뀐 채 그대로 유지한다.

## 파일 구조

```
Core/Interaction/                        (신규 폴더)
  IInteractable.cs                        (Core/Pickup/IPickupable.cs에서 이동+개명)
  WorldInteractionManager.cs               (Core/Pickup/WorldPickupManager.cs에서 이동+개명)
Core/Pickup/
  WorldPickup.cs                            (기존 파일 수정 - IInteractable 직접 구현)
  WeaponPickup.cs                            (기존 파일 수정 - 시그니처 변경)
  SkillPickup.cs                              (기존 파일 수정 - 시그니처 변경)
Player/
  Player.cs                                    (기존 파일 수정 - 필드 타입 변경)
  PlayerInteractionController.cs                (Player/PlayerPickupController.cs에서 개명)
Player/Input/
  InputHandler.cs                                (기존 파일 수정 - 메서드 호출부 이름만 변경)
Player/UI/
  InteractPromptUI.cs                              (Player/UI/PickupPromptUI.cs에서 개명)
```

## 컴포넌트

### `IInteractable` (신규, `IPickupable` 대체)

```csharp
public enum InteractChoice { Primary, Secondary }

public interface IInteractable
{
    string DisplayName { get; }
    Sprite Icon { get; }
    Transform Transform { get; }
    bool NeedsChoice(Player player);
    void Interact(Player player, InteractChoice choice);
}
```

- `Transform` 프로퍼티가 신규 추가된다. 매니저가 구체 타입(`WorldPickup`)을 몰라도 위치를 얻기 위함이다. MonoBehaviour 구현체는 `public Transform Transform => transform;`으로 충분하다.
- `Interact()`는 실제 적용까지만 책임지고, 이후 자신을 파괴할지 여부는 호출자(`WorldPickup` 자신)가 별도로 결정한다 — 인터페이스 계약에는 포함하지 않는다.

### `WorldInteractionManager` (신규, `WorldPickupManager` 대체)

```csharp
public class WorldInteractionManager : Singleton<WorldInteractionManager>
{
    [SerializeField] private WorldPickup pickupPrefab;
    private readonly List<IInteractable> interactables = new();

    public void Register(IInteractable interactable) => interactables.Add(interactable);
    public void Unregister(IInteractable interactable) => interactables.Remove(interactable);

    public IInteractable GetNearestInRange(Vector3 position, float radius)
    {
        IInteractable nearest = null;
        float nearestSqr = radius * radius;
        foreach (var i in interactables)
        {
            if (i == null) continue;
            float sqr = (i.Transform.position - position).sqrMagnitude;
            if (sqr > nearestSqr) continue;
            nearest = i;
            nearestSqr = sqr;
        }
        return nearest;
    }

    public void Spawn(IInteractable payload, Vector3 position)
    {
        var instance = Instantiate(pickupPrefab, position, Quaternion.identity);
        instance.Init(payload);
    }

    public void ClearAll()
    {
        foreach (var i in interactables)
        {
            if (i is WorldPickup wp) Destroy(wp.gameObject);
        }
        interactables.Clear();
    }
}
```

- `Register`/`Unregister`/`GetNearestInRange`는 완전히 일반화된다.
- `Spawn`/`ClearAll`은 여전히 `WorldPickup`을 구체적으로 알아야 한다 — "필드에 드롭된 픽업을 스폰/일괄 제거"는 픽업 컨테이너 개념 자체가 픽업 전용이기 때문이다(문은 스폰되거나 일괄 제거될 대상이 아님). 문/NPC가 늘어나도 이 두 메서드는 그대로 픽업 전용으로 남는다.

### `WorldPickup` (기존 파일 수정 - `IInteractable` 직접 구현)

```csharp
public class WorldPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private Weapon weaponAsset;
    [SerializeField] private SkillData skillAsset;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private IInteractable payload;

    public string DisplayName => payload.DisplayName;
    public Sprite Icon => payload.Icon;
    public Transform Transform => transform;

    private void Awake()
    {
        if (weaponAsset != null) payload = new WeaponPickup(weaponAsset);
        else if (skillAsset != null) payload = new SkillPickup(skillAsset);

        ApplyIcon();
        WorldInteractionManager.Instance.Register(this);
    }

    private void OnDestroy() => WorldInteractionManager.Instance.Unregister(this);

    public void Init(IInteractable payload)
    {
        this.payload = payload;
        ApplyIcon();
    }

    public bool NeedsChoice(Player player) => payload.NeedsChoice(player);

    public void Interact(Player player, InteractChoice choice)
    {
        payload.Interact(player, choice);
        Destroy(gameObject);
    }

    private void ApplyIcon()
    {
        if (payload?.Icon != null) spriteRenderer.sprite = payload.Icon;
    }
}
```

- 기존에 `Payload` 프로퍼티로 외부에 노출하던 것을 제거하고, `WorldPickup` 스스로 `IInteractable`을 구현해 위임하는 형태로 바뀐다(컨트롤러가 더 이상 `.Payload.Interact(...)`를 직접 호출하지 않으므로).
- 파괴 책임이 여기로 이동한 것이 이번 변경의 핵심이다.

### `WeaponPickup` / `SkillPickup` (기존 파일 수정 - 시그니처만 변경)

```csharp
public class WeaponPickup : IInteractable
{
    private readonly Weapon weapon;
    public WeaponPickup(Weapon weapon) => this.weapon = weapon;

    public string DisplayName => weapon.name;
    public Sprite Icon => weapon.icon;
    public Transform Transform => null; // 컨테이너(WorldPickup)를 통해서만 등록되므로 사용되지 않음
    public bool NeedsChoice(Player player) => false;

    public void Interact(Player player, InteractChoice choice)
    {
        var previous = player.Weapon;
        player.EquipWeapon(weapon);
        if (previous != null)
            WorldInteractionManager.Instance.Spawn(new WeaponPickup(previous), player.transform.position);
    }
}
```

`SkillPickup`도 동일하게 `Pickup` → `Interact`, `dropPosition` 파라미터 제거 후 `player.transform.position` 직접 참조로 바뀐다. 내부 로직(`NeedsChoice`, 슬롯 인덱스 계산)은 변경 없음.

`Transform`은 `WeaponPickup`/`SkillPickup`처럼 매니저에 직접 등록되지 않고 `WorldPickup`을 통해서만 쓰이는 구현체에서는 의미가 없다 — `null`을 반환하고 절대 호출되지 않는다. 이 어색함은 "인터페이스 하나가 두 가지 역할(직접 등록되는 감지 대상 vs. 컨테이너 내부 payload)을 겸한다"는 근본 원인에서 온다. 범위를 벗어나지 않는 선에서는 이 정도 트레이드오프로 두되, 만약 나중에 이런 payload-only 구현체가 늘어나면 `IInteractable`을 감지용(`ITransform` 포함)과 payload용으로 쪼개는 걸 재검토할 수 있다.

### `PlayerInteractionController` (`PlayerPickupController`에서 개명)

```csharp
public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private InteractPromptUI prompt;
    [SerializeField] private float detectRadius = 1.5f;
    [SerializeField] private float tapThreshold = 0.15f;
    [SerializeField] private float fillStartDelay = 0.5f;
    [SerializeField] private float holdDuration = 0.6f;

    private IInteractable current;
    private bool isHolding;
    private float pressStartTime;

    // UpdateNearest / UpdateHold / OnInteractStarted / OnInteractCanceled: 로직 변경 없음, 타입만 IInteractable로 교체

    private void Resolve(InteractChoice choice)
    {
        var target = current;
        current = null;
        prompt.Hide();
        target.Interact(player, choice);
    }
}
```

`Resolve()`에서 `Unregister`/`Destroy` 호출이 사라진 것이 유일한 실질 변경이다 — `WorldPickup.Interact()`가 자체적으로 처리하므로.

### `InteractPromptUI` (`PickupPromptUI`에서 개명, 내용 변경 없음)

## 예외 처리 / 경계 조건

- 기존 픽업 설계 문서의 경계 조건(홀드 중 대상 전환 금지, 스킬 슬롯 둘 다 빈 경우 즉시 index 0 배정, 무기 최초 장착 시 드롭 없음, 동률 거리 처리 없음)은 변경 없이 그대로 유지된다.
- `Interact()` 호출 후 대상을 파괴할지는 이제 각 구현체 책임이므로, 향후 문/NPC 구현 시 실수로 `Destroy`를 넣지 않도록 주의가 필요하다 — 이 설계에서 강제하는 장치는 없다(인터페이스 계약이 아니므로). 문서화 이상의 안전장치는 이번 범위 밖.
- `WorldInteractionManager.ClearAll()`은 여전히 `WorldPickup`만 파괴한다. 향후 문/NPC가 씬 전환 시 함께 정리되어야 한다면 별도 메커니즘이 필요하며, 이번 범위 밖이다.

## 다음 작업 (이번 범위 밖)

- `Door`, `Lever` 등 상태 토글형 인터랙터블 구현 (`MonoBehaviour, IInteractable` 직접 구현, `Interact()`에서 자신을 파괴하지 않음)
- NPC 대화/상점 인터랙터블 구현
- `IInteractable`이 감지용/payload용 두 역할을 겸하는 데서 오는 어색함(`WeaponPickup.Transform => null`)이 실제로 문제가 되면 인터페이스 분리 재검토
