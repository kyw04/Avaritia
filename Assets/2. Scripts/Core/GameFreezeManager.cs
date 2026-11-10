using UnityEngine;

public class GameFreezeManager : Singleton<GameFreezeManager>, IObserver<RunResetEvent>
{
    private int freezeCount;

    protected override void Awake()
    {
        base.Awake();
        EventBus.Subscribe<RunResetEvent>(this);
    }

    private void OnDestroy() => EventBus.UnsubscribeAll(this);

    // 런 초기화 시 정지·슬로모션이 남지 않게 한다.
    public void OnNotify(RunResetEvent e) => ResetAll();

    public void ResetAll()
    {
        freezeCount = 0;
        Time.timeScale = 1f;
    }

    public void Freeze()
    {
        freezeCount++;
        if (freezeCount == 1)
            Time.timeScale = 0f;
    }

    public void Unfreeze()
    {
        freezeCount = Mathf.Max(0, freezeCount - 1);
        if (freezeCount == 0)
            Time.timeScale = 1f;
    }
}
