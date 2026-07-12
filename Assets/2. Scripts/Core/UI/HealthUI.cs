using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour, IObserver<EntityHealthChangedEvent>
{
    private Image healthBarImage;
    private TextMeshProUGUI maxHealthText;
    private TextMeshProUGUI currentHealthText;
    [SerializeField] private Entity target;

    private void Awake()
    {
        healthBarImage = GetComponent<Image>();
        var comp = GetComponentsInChildren<TextMeshProUGUI>();
        if (comp.Length >= 2)
        {
            maxHealthText = GetComponentsInChildren<TextMeshProUGUI>()[0];
            currentHealthText = GetComponentsInChildren<TextMeshProUGUI>()[1];
        }
        
        EventBus.Subscribe<EntityHealthChangedEvent>(this);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<EntityHealthChangedEvent>(this);
    }

    public void OnNotify(EntityHealthChangedEvent e)
    {
        if (e.Source != target)
            return;

        float max = e.Max;
        float current = e.Current;
        if (maxHealthText != null)
            maxHealthText.text = max.ToString("#");
        if (currentHealthText != null)
            currentHealthText.text = current.ToString("#");
        
        healthBarImage.fillAmount = current / max;
    }
}
