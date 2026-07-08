using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHpUI : MonoBehaviour, IObserver<PlayerHealthChangedEvent>
{
    private Image healthBarImage;
    private TextMeshProUGUI maxHealthText;
    private TextMeshProUGUI currentHealthText;

    private void Awake()
    {
        healthBarImage = GetComponent<Image>();
        maxHealthText = GetComponentsInChildren<TextMeshProUGUI>()[0];
        currentHealthText = GetComponentsInChildren<TextMeshProUGUI>()[1];
        EventBus.Subscribe<PlayerHealthChangedEvent>(this);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<PlayerHealthChangedEvent>(this);
    }

    public void OnNotify(PlayerHealthChangedEvent e)
    {
        float max = e.Max;
        float current = e.Current;
        maxHealthText.text = max.ToString("#");
        currentHealthText.text = current.ToString("#");
        healthBarImage.fillAmount = current / max;
    }
}
