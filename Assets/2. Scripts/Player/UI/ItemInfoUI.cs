using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 아이콘, 이름, 설명을 보여 주는 정보 패널. 픽업, 인벤토리 등에서 같이 쓴다.
public class ItemInfoUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI descriptionLabel;

    public void Show(IInfo info)
    {
        icon.sprite = info.Icon;
        icon.enabled = info.Icon != null;
        nameLabel.text = info.DisplayName;
        descriptionLabel.text = info.Details;
        descriptionLabel.gameObject.SetActive(!string.IsNullOrEmpty(info.Details));
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);
}
