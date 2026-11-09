using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DashCountUI : MonoBehaviour, IObserver<EntityDashCountChangedEvent>
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image currentImage;
    [SerializeField] private TextMeshProUGUI currentText;
    [SerializeField] private ImageCountStyle colorGradient;

    [Header("Charged Punch")]
    [SerializeField] private float punchScale = 1.2f;
    [SerializeField, Min(0.01f)] private float punchDuration = 0.2f;

    private Player target;
    private Vector3 baseScale;
    private Color backgroundColor;
    private Color currentColor;
    private Coroutine punchCoroutine;
    private int currentCount = -1;
    private readonly Queue<float> starts = new(); // 대쉬마다 시작 시각
    private float segmentStart;
    private Coroutine dashImageCoroutine;

    private void Awake()
    {
        target = FindAnyObjectByType<Player>();
        baseScale = transform.localScale;
        EventBus.Subscribe<EntityDashCountChangedEvent>(this);
    }

    private void OnDestroy() => EventBus.Unsubscribe<EntityDashCountChangedEvent>(this);

    private void OnEnable() => StartFill();

    private void OnDisable()
    {
        if (dashImageCoroutine != null)
            StopCoroutine(dashImageCoroutine);
        dashImageCoroutine = null;

        if (punchCoroutine != null)
            StopCoroutine(punchCoroutine);
        EndPunch();
    }

    private void StartFill()
    {
        if (starts.Count > 0 && isActiveAndEnabled)
            dashImageCoroutine ??= StartCoroutine(DashImageCountCoroutine());
    }
    
    private IEnumerator DashImageCountCoroutine()
    {
        while (starts.Count > 0)
        {
            float end = starts.Peek() + target.DashCooldown;
            float length = end - segmentStart;

            while (Time.time < end)
            {
                currentImage.fillAmount = length > 0f ? (Time.time - segmentStart) / length : 1f;
                yield return null;
            }
            starts.Dequeue();
            segmentStart = Time.time;
            Punch();
        }

        currentImage.fillAmount = 1f;
        dashImageCoroutine = null;
    }

    private void Punch()
    {
        if (punchCoroutine != null)
            StopCoroutine(punchCoroutine);
        punchCoroutine = StartCoroutine(PunchCoroutine());
    }

    private IEnumerator PunchCoroutine()
    {
        for (float t = 0f; t < punchDuration; t += Time.deltaTime)
        {
            float k = t / punchDuration;
            transform.localScale = baseScale * Mathf.Lerp(punchScale, 1f, k);
            backgroundImage.color = Color.Lerp(Color.white, backgroundColor, k);
            currentImage.color = Color.Lerp(Color.white, currentColor, k);
            yield return null;
        }
        EndPunch();
    }

    private void EndPunch()
    {
        punchCoroutine = null;
        transform.localScale = baseScale;
        backgroundImage.color = backgroundColor;
        currentImage.color = currentColor;
    }

    public void OnNotify(EntityDashCountChangedEvent e)
    {
        if (e.Source != target)
            return;

        backgroundColor = colorGradient.EvaluateFillColor(e.Current, e.Max);
        currentColor = colorGradient.EvaluateFillColor(e.Current + 1, e.Max);
        if (punchCoroutine == null)
        {
            backgroundImage.color = backgroundColor;
            currentImage.color = currentColor;
        }
        currentText.text = e.Current.ToString();
        
        if (currentCount > e.Current)
        {
            if (starts.Count == 0)
                segmentStart = e.StartTime;
            for (int i = e.Current; i < currentCount; i++)
                starts.Enqueue(e.StartTime);
            StartFill();
        }
        currentCount = e.Current;
    }
}
