using UnityEngine;

// ImageCountBar 게이지의 모양 설정. 무기 모듈이 바에 넘겨 준다.
[CreateAssetMenu(fileName = "ImageCountStyle", menuName = "Scriptable Objects/UI/ImageCountStyle")]
public class ImageCountStyle : ScriptableObject
{
    [SerializeField] private Color color = Color.white;
    // 켜면 채워진 비율(현재 개수 / 최대)에 따라 이 그라데이션 색을 칠한다.
    [SerializeField] private bool useGradient;
    [SerializeField] private Gradient gradient = new();
    // 칸 사이 틈(각도). 0이면 틈 없이 이어진다.
    [SerializeField, Min(0f)] private float gap = 1f;

    public float Gap => gap;

    public Color EvaluateFillColor(int currentCount, int maxCount)
    {
        if (!useGradient)
            return color;
        return gradient.Evaluate(maxCount > 0 ? (float)currentCount / maxCount : 0f);
    }
}
