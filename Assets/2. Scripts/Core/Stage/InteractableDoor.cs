using UnityEngine;

public class InteractableDoor : MonoBehaviour, IInteractable, IObserver<StageNodeClearedEvent>
{
    [SerializeField] private RoomType roomType;
    [SerializeField] private BattleRoomTypeFilter battleRoomTypeFilter;
    [SerializeField] private StageData targetStage;

    private WorldInteractionManager manager;
    private bool isRegistered;

    public string DisplayName => "문";
    public Sprite Icon => null;
    public Transform Transform => transform;

    private void Awake()
    {
        manager = WorldInteractionManager.Instance;
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
    }

    public bool NeedsChoice(Player player) => false;

    public void Interact(Player player, InteractChoice choice)
    {
        if (targetStage == null)
            StageManager.Instance.MoveTo(StageManager.Instance.GetStage(roomType, battleRoomTypeFilter));
        else if (targetStage != StageManager.Instance.CurrentStageData)
            StageManager.Instance.AdvanceStage(targetStage);
    }
}
