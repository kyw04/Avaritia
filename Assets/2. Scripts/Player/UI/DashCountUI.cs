using TMPro;
using UnityEngine;

public class DashCountUI : MonoBehaviour, IObserver<EntityDashCountChangedEvent>
{
    [SerializeField] private TextMeshProUGUI maxCountText;
    [SerializeField] private TextMeshProUGUI countText;

    private Player target;

    private void Awake()
    {
        target = FindAnyObjectByType<Player>();
        var comp = GetComponentsInChildren<TextMeshProUGUI>();
        if (comp.Length >= 2)
        {
            maxCountText = GetComponentsInChildren<TextMeshProUGUI>()[0];
            countText = GetComponentsInChildren<TextMeshProUGUI>()[1];
        }
        EventBus.Subscribe<EntityDashCountChangedEvent>(this);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<EntityDashCountChangedEvent>(this);
    }

    public void OnNotify(EntityDashCountChangedEvent e)
    {
        if (e.Source != target)
            return;

        if (maxCountText != null)
            maxCountText.text = e.Max.ToString();
        if (countText != null)
            countText.text = e.Current.ToString();
    }
}
