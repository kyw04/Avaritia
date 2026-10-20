using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public class ImageCountPoolable : MonoBehaviour, IPoolable
{
    private RectTransform rectTransform;
    private Image image;
    public Sprite Icon { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    public void Set(ImageCountPoolable prefab)
    {
        rectTransform = prefab.rectTransform;
        
        Icon = prefab.Icon;
        image.sprite = Icon;
    }
    
    public void OnSpawn() { }
    public void OnDespawn() { }
}