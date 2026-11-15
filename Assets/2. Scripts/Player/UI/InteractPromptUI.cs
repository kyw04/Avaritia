using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractPromptUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI keyLabel;
    // 키 표시 위에 띄우는 픽업 정보. 키 표시(root)를 숨기면 함께 숨는다.
    [SerializeField] private ItemInfoUI info;

    public ItemInfoUI Info => info;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
    }

    public void Show(string keyText)
    {
        if (keyLabel != null) keyLabel.text = keyText;
        if (root != null) root.SetActive(true);
        SetProgress(0f);
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    public void SetProgress(float t)
    {
        if (fillImage != null) fillImage.fillAmount = Mathf.Clamp01(t);
    }
}
