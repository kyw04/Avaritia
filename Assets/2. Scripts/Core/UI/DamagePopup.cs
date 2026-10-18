using TMPro;
using UnityEngine;

// 월드 공간 데미지 텍스트. 위로 튀어 올랐다가 중력으로 떨어지며 사라진다.
[RequireComponent(typeof(TextMeshPro))]
public class DamagePopup : MonoBehaviour, IPoolable
{
    [Header("Motion")]
    [SerializeField] private float upSpeed = 6f;           // 처음 튀어 오르는 속도
    [SerializeField] private float horizontalSpread = 1.5f; // 좌우로 랜덤하게 퍼지는 최대 속도
    [SerializeField] private float gravity = 18f;
    [SerializeField, Min(0.01f)] private float lifetime = 0.8f;
    [SerializeField, Min(0)] private float fadeDuration = 0.3f; // 수명 끝에서 페이드아웃되는 시간

    [Header("Punch")]
    [SerializeField, Min(0)] private float punchDuration = 0.1f; // 생성 직후 커졌다가 원래 크기로 돌아오는 시간
    [SerializeField, Min(1)] private float punchScale = 1.4f;

    [Header("Rendering")]
    [SerializeField] private int sortingOrder = 100;

    private TextMeshPro text;
    private Vector3 velocity;
    private Vector3 baseScale;
    private Color color;
    private float elapsed;

    private void Awake()
    {
        text = GetComponent<TextMeshPro>();
        text.sortingOrder = sortingOrder;
    }

    public void Show(string content, DamagePopupStyle style)
    {
        text.text = content;
        color = style.color;
        text.color = color;
        baseScale = Vector3.one * style.scale;
        transform.localScale = baseScale * punchScale;
        velocity = new Vector3(Random.Range(-horizontalSpread, horizontalSpread), upSpeed, 0f);
        elapsed = 0f;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= lifetime)
        {
            ObjectPoolManager.Instance.Despawn(this);
            return;
        }

        velocity.y -= gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        float punchT = punchDuration > 0f ? Mathf.Clamp01(elapsed / punchDuration) : 1f;
        transform.localScale = baseScale * Mathf.Lerp(punchScale, 1f, punchT);

        float remaining = lifetime - elapsed;
        var c = color;
        c.a = fadeDuration > 0f ? color.a * Mathf.Clamp01(remaining / fadeDuration) : color.a;
        text.color = c;
    }

    public void OnSpawn() { }
    public void OnDespawn() { }
}
