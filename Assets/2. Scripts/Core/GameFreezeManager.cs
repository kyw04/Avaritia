using UnityEngine;

public class GameFreezeManager : Singleton<GameFreezeManager>,
    IObserver<InventoryUIOnEvent>, IObserver<InventoryUIOffEvent>
{
    private int freezeCount;

    protected override void Awake()
    {
        base.Awake();
        EventBus.Subscribe<InventoryUIOnEvent>(this);
        EventBus.Subscribe<InventoryUIOffEvent>(this);
    }

    public void OnNotify(InventoryUIOnEvent e) => Freeze();
    public void OnNotify(InventoryUIOffEvent e) => Unfreeze();

    private void Freeze()
    {
        freezeCount++;
        if (freezeCount == 1)
            Time.timeScale = 0f;
    }

    private void Unfreeze()
    {
        freezeCount = Mathf.Max(0, freezeCount - 1);
        if (freezeCount == 0)
            Time.timeScale = 1f;
    }
}
