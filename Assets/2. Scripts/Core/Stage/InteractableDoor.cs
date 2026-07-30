using UnityEngine;

public class InteractableDoor : MonoBehaviour, IInteractable, IObserver<StageNodeClearedEvent>
{
    [SerializeField] private StageNode targetNode;
    [SerializeField] private StageData targetStage;

    private bool isRegistered;

    public string DisplayName => "문";
    public Sprite Icon => null;
    public Transform Transform => transform;

    private void Awake()
    {
        EventBus.Subscribe<StageNodeClearedEvent>(this);
        if (StageManager.Instance.IsCurrentRoomCleared)
            RegisterSelf();
    }

    private void OnDestroy()
    {
        EventBus.UnsubscribeAll(this);
        if (isRegistered)
            WorldInteractionManager.Instance.Unregister(this);
    }

    public void OnNotify(StageNodeClearedEvent e) => RegisterSelf();

    private void RegisterSelf()
    {
        if (isRegistered) return;
        isRegistered = true;
        WorldInteractionManager.Instance.Register(this);
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
