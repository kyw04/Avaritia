using UnityEngine;
using UnityEngine.UI;

public class BossHpUI : MonoBehaviour, IObserver<BossHealthChangedEvent>
{
    private Image healthBarImage;

    private void Awake()
    {
        healthBarImage = GetComponent<Image>();
        EventBus.Subscribe<BossHealthChangedEvent>(this);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<BossHealthChangedEvent>(this);
    }

    public void OnNotify(BossHealthChangedEvent e)
    {
        healthBarImage.fillAmount = e.Ratio;
    }
}
