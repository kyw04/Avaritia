using UnityEngine;

// 총알이 날아갈 직선 궤도를 빨간 선으로 미리 보여 준다. 풀에서 꺼내 Show, 다 쓰면 Hide.
// 9픽셀 두께로 시작해 준비 시간 동안 바깥 줄부터 벗겨져 1픽셀 선이 되면 한 번 반짝인다.
// 쏘는 쪽이 사라지면(사망 후 반납 등) 스스로 숨는다.
[RequireComponent(typeof(SpriteRenderer))]
public class TrajectoryTelegraph : MonoBehaviour, IPoolable
{
    private const int MaxThickness = 9;
    private const float PixelsPerUnit = 32f;
    // 가운데 3줄은 진하고, 위아래 바깥 3줄은 바깥으로 갈수록 연하다.
    private static readonly float[] RowAlpha = { 0.25f, 0.45f, 0.7f, 1f, 1f, 1f, 0.7f, 0.45f, 0.25f };
    private static Sprite[] spritesByThickness;
    // 1픽셀이 되는 순간부터 이 시간 동안 흰색으로 반짝인다.
    private const float FlashDuration = 0.1f;

    [SerializeField] private SpriteRenderer line;

    private MonoBehaviour owner;
    private float startTime;
    private float duration;
    private Color baseColor;

    private void Awake() => baseColor = line.color;

    public void Show(MonoBehaviour shooter, Vector3 origin, Vector2 direction, float length, float shrinkDuration)
    {
        owner = shooter;
        startTime = Time.time;
        duration = shrinkDuration;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.SetPositionAndRotation(origin, Quaternion.Euler(0f, 0f, angle));
        // 스프라이트는 가로 1픽셀이라 길이만큼 가로로 늘린다.
        transform.localScale = new Vector3(length * PixelsPerUnit, 1f, 1f);
        line.sprite = GetSprite(MaxThickness);
        line.color = baseColor;
    }

    public void Hide()
    {
        if (gameObject.activeSelf)
            ObjectPoolManager.Instance.Despawn(gameObject);
    }

    private void Update()
    {
        if (owner == null || !owner.isActiveAndEnabled)
        {
            Hide();
            return;
        }

        // 9, 7, 5, 3, 1 다섯 단계로 나눠, 준비가 끝날 즈음 1픽셀이 되게 한다.
        float elapsed = Time.time - startTime;
        float progress = duration > 0f ? elapsed / duration : 1f;
        int steps = MaxThickness / 2;
        int step = Mathf.Min(steps, (int)(progress * (steps + 1)));
        line.sprite = GetSprite(MaxThickness - step * 2);

        // 1픽셀이 되는 순간 밝아졌다가 원래 색으로 돌아온다.
        float thinAt = duration * steps / (steps + 1);
        float t = (elapsed - thinAt) / FlashDuration;
        line.color = t >= 0f && t < 1f
            ? Color.Lerp(baseColor, Color.white, 1f - Mathf.Abs(t * 2f - 1f))
            : baseColor;
    }

    // 두께별 스프라이트. 9줄 텍스처의 가운데 thickness줄만 잘라 쓰므로 바깥 줄부터 사라진다.
    private static Sprite GetSprite(int thickness)
    {
        if (spritesByThickness == null)
        {
            var texture = new Texture2D(1, MaxThickness, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            for (int y = 0; y < MaxThickness; y++)
                texture.SetPixel(0, y, new Color(1f, 1f, 1f, RowAlpha[y]));
            texture.Apply();

            spritesByThickness = new Sprite[MaxThickness + 1];
            for (int t = 1; t <= MaxThickness; t += 2)
                spritesByThickness[t] = Sprite.Create(texture, new Rect(0, (MaxThickness - t) / 2, 1, t),
                    new Vector2(0f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
        }
        return spritesByThickness[thickness];
    }

    public void OnSpawn() { }

    public void OnDespawn() => owner = null;
}
