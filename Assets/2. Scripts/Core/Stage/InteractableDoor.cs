using UnityEngine;

public class InteractableDoor : MonoBehaviour, IInteractable, IObserver<StageNodeClearedEvent>
{
    [SerializeField] private RoomType roomType;
    [SerializeField] private BattleRoomTypeFilter battleRoomTypeFilter;
    [SerializeField] private StageData targetStage;
    // 지정하면 방 종류 대신 이 방으로 바로 간다(스테이지 데이터에 없는 테스트 방 등).
    [SerializeField] private StageNode targetNode;
    [SerializeField] private Color specialColor = new Color(1f, 0.7255f, 0.6f);

    private WorldInteractionManager manager;
    private SpriteRenderer spriteRenderer;
    private bool isRegistered;

    public string DisplayName => "문";
    public Sprite Icon => null;
    public string Details => null;
    public Transform Transform => transform;

    private void Awake()
    {
        manager = WorldInteractionManager.Instance;
        spriteRenderer = GetComponent<SpriteRenderer>();
        EventBus.Subscribe<StageNodeClearedEvent>(this);
        if (StageManager.Instance.IsCurrentRoomCleared)
            RegisterSelf();
    }

    private void OnDestroy()
    {
        EventBus.UnsubscribeAll(this);
        if (isRegistered)
            manager.Unregister(this);
    }

    public void OnNotify(StageNodeClearedEvent e) => RegisterSelf();

    private void RegisterSelf()
    {
        if (isRegistered) return;
        isRegistered = true;
        manager.Register(this);

        if (roomType == RoomType.Battle && StageManager.Instance.HasPendingSpecialNode)
            spriteRenderer.color = specialColor;
    }

    public bool NeedsChoice(Player player) => false;

    public void Interact(Player player, InteractChoice choice)
    {
        if (targetNode != null)
            StageManager.Instance.MoveTo(targetNode);
        else if (targetStage == null)
            StageManager.Instance.MoveTo(StageManager.Instance.GetStage(roomType, battleRoomTypeFilter));
        else if (targetStage != StageManager.Instance.CurrentStageData)
            StageManager.Instance.AdvanceStage(targetStage);
    }
}
