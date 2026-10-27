using UnityEngine;

public class GameFreezeManager : Singleton<GameFreezeManager>
{
    private int freezeCount;

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
