using TMPro;
using UnityEngine;

public class DashCountUI : MonoBehaviour, IObserver<PlayerDashCountChangedEvent>
{
    [SerializeField] private TextMeshProUGUI maxCountText;
    [SerializeField] private TextMeshProUGUI countText;

    private void Awake()
    {
        EventBus.Subscribe<PlayerDashCountChangedEvent>(this);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<PlayerDashCountChangedEvent>(this);
    }

    public void OnNotify(PlayerDashCountChangedEvent e)
    {
        maxCountText.text = e.Max.ToString();
        countText.text = e.Current.ToString();
    }
}
