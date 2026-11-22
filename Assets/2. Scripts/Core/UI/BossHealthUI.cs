using TMPro;
using UnityEngine;

// 보스가 스폰되면(보스방 입장) 보스 체력 UI를 켜고 그 보스에 연결한다.
// 보스가 풀에 돌아가면(사망 모션 종료·방 이동·다시 시작) 끈다. 항상 켜져 있는 오브젝트에 붙인다.
public class BossHealthUI : MonoBehaviour, IObserver<BossSpawnedEvent>, IObserver<BossDespawnedEvent>
{
    [SerializeField] private GameObject root;
    [SerializeField] private HealthUI healthUI;
    [SerializeField] private TextMeshProUGUI nameText;

    private Boss current;

    private void Awake()
    {
        EventBus.Subscribe<BossSpawnedEvent>(this);
        EventBus.Subscribe<BossDespawnedEvent>(this);
        root.SetActive(false);
    }

    private void OnDestroy() => EventBus.UnsubscribeAll(this);

    public void OnNotify(BossSpawnedEvent e)
    {
        current = e.Source;
        // 켠 뒤에 연결해야 HealthUI가 Awake에서 체력 변경 이벤트를 구독한다.
        root.SetActive(true);
        healthUI.SetTarget(current);
        if (nameText != null)
            nameText.text = current.DisplayName;
    }

    public void OnNotify(BossDespawnedEvent e)
    {
        if (e.Source != current)
            return;

        current = null;
        healthUI.SetTarget(null);
        root.SetActive(false);
    }
}
