using UnityEngine;

public class InteractableDoor : MonoBehaviour, IInteractable, IObserver<StageNodeClearedEvent>
{
    [SerializeField] private StageNode targetNode;
    [SerializeField] private StageData targetStage;

    private WorldInteractionManager manager;
    private bool isRegistered;

    public string DisplayName => "문";
    public Sprite Icon => null;
    public Transform Transform => transform;

    private void Awake()
    {
        if (targetNode == null && targetStage == null)
            Debug.LogError($"InteractableDoor: neither targetNode nor targetStage is assigned on {name}");

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
        if (targetStage != null)
            StageManager.Instance.AdvanceStage(targetStage);
        else
            StageManager.Instance.MoveTo(targetNode);
    }
}
