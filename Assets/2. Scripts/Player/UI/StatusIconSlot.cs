using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상태 아이콘 한 칸: 아이콘 + 남은 시간 채움 오버레이 + 숫자(스택 수 / 실드 잔량 / 무효화 횟수).
public class StatusIconSlot : MonoBehaviour, IPoolable
{
    [SerializeField] private Image icon;
    [SerializeField] private Image fill;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Color cooldownTint = new(0.45f, 0.45f, 0.45f, 1f);

    private int shownValue = int.MinValue;

    public (object source, StatusIconKind kind) Key { get; private set; }

    public void Set(StatusIconEntry entry)
    {
        Key = entry.Key;
        icon.sprite = entry.Icon;
        icon.color = entry.Kind == StatusIconKind.Cooldown ? cooldownTint : Color.white;

        fill.enabled = entry.IsTimed;
        if (entry.IsTimed)
            fill.fillAmount = Mathf.Clamp01((entry.EndTime - Time.time) / entry.Duration);

        // 값이 바뀔 때만 문자열을 새로 만든다.
        if (label != null && entry.Count != shownValue)
        {
            shownValue = entry.Count;
            label.text = entry.Count > 0 ? entry.Count.ToString() : string.Empty;
        }
    }

    public void OnSpawn() => shownValue = int.MinValue;
    public void OnDespawn() { }
}
