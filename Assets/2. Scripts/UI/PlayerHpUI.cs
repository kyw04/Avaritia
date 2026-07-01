using UnityEngine;
using UnityEngine.UI;

public class PlayerHpUI : MonoBehaviour
{
    private static Image healthBarImage;

    private void Awake()
    {
        healthBarImage = GetComponent<Image>();
    }

    public static void UpdateHealthBar(float value)
    {
        healthBarImage.fillAmount = value;
    }
}
