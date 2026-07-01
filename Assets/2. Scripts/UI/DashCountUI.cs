using TMPro;
using UnityEngine;

public class DashCountUI : MonoBehaviour
{
    private Player player;
    [SerializeField] private TextMeshProUGUI maxCountText;
    [SerializeField] private TextMeshProUGUI countText;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Update()
    {
        int count = player.MaxDashCount - player.DashCount;
        maxCountText.text = player.MaxDashCount.ToString();
        countText.text = count.ToString();
    }
}
