using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI를 순간 키우고 flashColor로 번쩍인 뒤 duration 동안 원래 크기·색으로 되돌린다.
// 이미지는 flashMaterial(Avaritia/SpriteFlash)로 픽셀 색 자체를 섞으므로 Image.color가 흰색이어도 번쩍인다.
// TMP 텍스트는 셰이더가 달라 글자 색(color)을 섞는다. 번쩍이는 동안 텍스트 색을 바꾸려면 SetColor를 쓴다.
public class UIPunch : MonoBehaviour
{
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

    [SerializeField] private float scale = 1.2f; // 1이면 크기는 그대로
    [SerializeField, Min(0.01f)] private float duration = 0.2f;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private Material flashMaterial; // Avaritia/SpriteFlash 머티리얼(M_HitFlash)
    [SerializeField] private Graphic[] targets; // 비우면 번쩍이지 않는다

    private readonly Dictionary<Graphic, Material> originalMaterials = new();
    private readonly Dictionary<Graphic, Color> baseColors = new();
    private Material flashInstance;
    private Vector3 baseScale;
    private Coroutine routine;

    private void Awake()
    {
        baseScale = transform.localScale;
        // 같은 머티리얼을 쓰는 다른 UIPunch와 섞이지 않도록 복사본을 쓴다.
        if (flashMaterial != null)
            flashInstance = new Material(flashMaterial);
    }

    private void OnDestroy()
    {
        if (flashInstance != null)
            Destroy(flashInstance);
    }

    private void OnDisable()
    {
        if (routine != null)
            StopCoroutine(routine);
        End();
    }

    public void Play()
    {
        if (!isActiveAndEnabled)
            return;

        if (routine == null)
            Begin();
        else
            StopCoroutine(routine);
        routine = StartCoroutine(PlayRoutine());
    }

    // 번쩍이는 중인 텍스트면 끝난 뒤 돌아갈 색으로 저장하고, 아니면 바로 칠한다.
    public void SetColor(Graphic graphic, Color color)
    {
        if (routine != null && baseColors.ContainsKey(graphic))
            baseColors[graphic] = color;
        else
            graphic.color = color;
    }

    private void Begin()
    {
        originalMaterials.Clear();
        baseColors.Clear();
        if (flashInstance != null)
            flashInstance.SetColor(FlashColorId, flashColor);

        foreach (var g in targets)
        {
            if (g == null)
                continue;
            if (g is TMP_Text || flashInstance == null)
            {
                baseColors[g] = g.color;
            }
            else
            {
                originalMaterials[g] = g.material;
                g.material = flashInstance;
            }
        }
    }

    private IEnumerator PlayRoutine()
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            Apply(Mathf.Lerp(scale, 1f, k), 1f - k);
            yield return null;
        }
        End();
    }

    private void Apply(float scaleFactor, float flash)
    {
        transform.localScale = baseScale * scaleFactor;
        if (flashInstance != null)
            flashInstance.SetFloat(FlashAmountId, flash);
        foreach (var (g, c) in baseColors)
            g.color = Color.Lerp(c, flashColor, flash);
    }

    private void End()
    {
        if (routine == null)
            return;
        routine = null;
        transform.localScale = baseScale;
        foreach (var (g, m) in originalMaterials)
            if (g != null)
                g.material = m == g.defaultMaterial ? null : m;
        foreach (var (g, c) in baseColors)
            if (g != null)
                g.color = c;
    }
}
