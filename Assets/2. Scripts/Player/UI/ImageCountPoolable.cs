using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public class ImageCountPoolable : MonoBehaviour, IPoolable
{
    private RectTransform rectTransform;
    private Image image;
    private Sprite defaultSprite;
    private Color defaultColor;
    // 켜면 바의 칸 위치(첫 칸 0 ~ 마지막 칸 1)에 따라 이 그라데이션 색을 칠한다.
    [SerializeField] private bool useGradient;
    // 켜면 칸 위치 대신 채워진 비율(현재 개수 / 최대)로 모든 칸을 같은 색으로 칠한다.
    [SerializeField] private bool gradientByFill;
    [SerializeField] private Gradient gradient = new();
    public Sprite Icon { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        defaultSprite = image.sprite;
        defaultColor = image.color;
    }

    // sprite가 null이면 기본 이미지로 되돌린다.
    public void SetIcon(Sprite sprite) => image.sprite = sprite ? sprite : defaultSprite;

    // 그라데이션을 쓰지 않으면 기본 색으로 되돌린다.
    public void ApplyGradient(int index, int currentCount, int maxCount)
    {
        if (!useGradient)
        {
            image.color = defaultColor;
            return;
        }

        float t = gradientByFill
            ? (maxCount > 0 ? (float)currentCount / maxCount : 0f)
            : (maxCount > 1 ? (float)index / (maxCount - 1) : 1f);
        image.color = gradient.Evaluate(t);
    }

    public void Set(ImageCountPoolable prefab)
    {
        rectTransform = prefab.rectTransform;

        Icon = prefab.Icon;
        image.sprite = Icon;
        // 바가 칸을 재사용하므로 다른 무기의 프리팹으로 바뀔 때 그라데이션 설정도 따라가야 한다.
        useGradient = prefab.useGradient;
        gradientByFill = prefab.gradientByFill;
        gradient = prefab.gradient;
    }
    
    public void OnSpawn() { }
    public void OnDespawn() { }
}