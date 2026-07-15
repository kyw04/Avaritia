using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PickupPromptUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI keyLabel;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
    }

    public void Show()
    {
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
