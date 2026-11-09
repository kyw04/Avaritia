using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour, IObserver<EntityHealthChangedEvent>
{
    [SerializeField] private Entity target;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image healthBarImage;
    [SerializeField] private TextMeshProUGUI maxHealthText;
    [SerializeField] private TextMeshProUGUI currentHealthText;
    [SerializeField] private UIPunch changedPunch;

    private void Awake()
    {
        EventBus.Subscribe<EntityHealthChangedEvent>(this);
        
        changedPunch.SetColor(backgroundImage, backgroundImage.color);
        changedPunch.SetColor(healthBarImage, healthBarImage.color);
    }
    
    private void OnDestroy() => EventBus.Unsubscribe<EntityHealthChangedEvent>(this);

    public void OnNotify(EntityHealthChangedEvent e)
    {
        if (e.Source != target)
            return;

        float max = e.Max;
        float current = e.Current;
        if (maxHealthText != null)
            maxHealthText.text = max.ToString("0");
        if (currentHealthText != null)
            currentHealthText.text = current.ToString("0");
        
        changedPunch.Play();
        healthBarImage.fillAmount = current / max;
    }
}
