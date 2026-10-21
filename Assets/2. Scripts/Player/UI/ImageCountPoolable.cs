using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public class ImageCountPoolable : MonoBehaviour, IPoolable
{
    private RectTransform rectTransform;
    private Image image;
    private Sprite defaultSprite;
    public Sprite Icon { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        defaultSprite = image.sprite;
    }

    // sprite가 null이면 기본 이미지로 되돌린다.
    public void SetIcon(Sprite sprite) => image.sprite = sprite ? sprite : defaultSprite;

    public void Set(ImageCountPoolable prefab)
    {
        rectTransform = prefab.rectTransform;
        
        Icon = prefab.Icon;
        image.sprite = Icon;
    }
    
    public void OnSpawn() { }
    public void OnDespawn() { }
}