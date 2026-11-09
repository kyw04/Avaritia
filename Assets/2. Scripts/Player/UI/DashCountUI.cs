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
    [SerializeField] private UIPunch chargedPunch;

    private Player target;
    private int currentCount = -1;
    private readonly Queue<float> starts = new(); // 대쉬마다 시작 시각
    private float segmentStart;
    private Coroutine dashImageCoroutine;

    private void Awake()
    {
        target = FindAnyObjectByType<Player>();
        EventBus.Subscribe<EntityDashCountChangedEvent>(this);
    }

    private void OnDestroy() => EventBus.Unsubscribe<EntityDashCountChangedEvent>(this);

    private void OnEnable() => StartFill();

    private void OnDisable()
    {
        if (dashImageCoroutine != null)
            StopCoroutine(dashImageCoroutine);
        dashImageCoroutine = null;
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
            chargedPunch.Play();
        }

        currentImage.fillAmount = 1f;
        dashImageCoroutine = null;
    }

    public void OnNotify(EntityDashCountChangedEvent e)
    {
        if (e.Source != target)
            return;

        chargedPunch.SetColor(backgroundImage, colorGradient.EvaluateFillColor(e.Current, e.Max));
        chargedPunch.SetColor(currentImage, colorGradient.EvaluateFillColor(e.Current + 1, e.Max));
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
