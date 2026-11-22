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

        Refresh(e.Max, e.Current);
        changedPunch.Play();
    }

    // 대상을 바꾸고(보스 등 런타임에 정해지는 대상) 지금 체력으로 바로 채운다.
    public void SetTarget(Entity newTarget)
    {
        target = newTarget;
        if (target != null)
            Refresh(target.MaxHealth, target.CurrentHealth);
    }

    private void Refresh(float max, float current)
    {
        if (maxHealthText != null)
            maxHealthText.text = max.ToString("0");
        if (currentHealthText != null)
            currentHealthText.text = current.ToString("0");
        healthBarImage.fillAmount = current / max;
    }
}
