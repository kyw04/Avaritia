using UnityEngine;
using UnityEngine.UI;

public class PlayerHpUI : MonoBehaviour, IObserver<PlayerHealthChangedEvent>
{
    private Image healthBarImage;

    private void Awake()
    {
        healthBarImage = GetComponent<Image>();
        EventBus.Subscribe<PlayerHealthChangedEvent>(this);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<PlayerHealthChangedEvent>(this);
    }

    public void OnNotify(PlayerHealthChangedEvent e)
    {
        healthBarImage.fillAmount = e.Ratio;
    }
}
