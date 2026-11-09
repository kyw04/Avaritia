using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// UI를 순간 키우고 flashColor로 번쩍인 뒤 duration 동안 원래 크기·색으로 되돌린다.
// 번쩍이는 동안 targets의 색을 바꾸려면 SetColor를 쓴다. 직접 칠하면 번쩍임이 덮어쓴다.
public class UIPunch : MonoBehaviour
{
    [SerializeField] private float scale = 1.2f;                 // 1이면 크기는 그대로
    [SerializeField, Min(0.01f)] private float duration = 0.2f;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private Graphic[] targets;                  // 비우면 번쩍이지 않는다

    private readonly Dictionary<Graphic, Color> baseColors = new();
    private Vector3 baseScale;
    private Coroutine routine;

    private void Awake() => baseScale = transform.localScale;

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
        {
            baseColors.Clear();
            foreach (var g in targets)
                if (g != null)
                    baseColors[g] = g.color;
        }
        else
        {
            StopCoroutine(routine);
        }
        routine = StartCoroutine(PlayRoutine());
    }

    // 번쩍이는 중이면 끝난 뒤 돌아갈 색으로 저장하고, 아니면 바로 칠한다.
    public void SetColor(Graphic graphic, Color color)
    {
        if (routine != null && baseColors.ContainsKey(graphic))
            baseColors[graphic] = color;
        else
            graphic.color = color;
    }

    private IEnumerator PlayRoutine()
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            transform.localScale = baseScale * Mathf.Lerp(scale, 1f, k);
            foreach (var (g, c) in baseColors)
                g.color = Color.Lerp(flashColor, c, k);
            yield return null;
        }
        End();
    }

    private void End()
    {
        if (routine == null)
            return;
        routine = null;
        transform.localScale = baseScale;
        foreach (var (g, c) in baseColors)
            if (g != null)
                g.color = c;
    }
}
