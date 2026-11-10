using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

// 플레이어 사망 연출: 히트스톱 → 카메라 흔들기 → 슬로모션 + 줌인 → 화면 어두워짐 → 결과 화면.
// timeScale을 직접 바꾸므로 대기는 모두 실제 시간 기준이다.
public class PlayerDeathSequence : MonoBehaviour, IObserver<PlayerDiedEvent>, IObserver<RunResetEvent>
{
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private Image fadeImage;

    [SerializeField] private float hitStopDuration = 0.08f;
    [SerializeField] private float shakeForce = 3f;
    [SerializeField] private float slowTimeScale = 0.3f;
    [SerializeField] private float slowDuration = 0.8f;
    [SerializeField, Range(0.1f, 1f)] private float zoomSizeRatio = 0.75f;
    [SerializeField] private float fadeDuration = 0.3f;
    // 결과 화면 오버레이와 같은 색으로 맞춰 화면이 끊김 없이 넘어가게 한다.
    [SerializeField] private Color fadeColor = new Color32(0x16, 0x11, 0x0e, 224);

    private float originalOrthoSize;
    private Coroutine routine;

    private void Awake()
    {
        originalOrthoSize = virtualCamera.Lens.OrthographicSize;
        SetFade(0f);
        EventBus.Subscribe<PlayerDiedEvent>(this);
        EventBus.Subscribe<RunResetEvent>(this);
    }

    private void OnDestroy() => EventBus.UnsubscribeAll(this);

    public void OnNotify(PlayerDiedEvent e)
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(Play());
    }

    public void OnNotify(RunResetEvent e)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        SetOrthoSize(originalOrthoSize);
        SetFade(0f);
    }

    private IEnumerator Play()
    {
        // 치명타의 피격 히트스톱이 끝나면서 timeScale을 1로 되돌리지 않게 먼저 멈춘다.
        cameraManager.StopSlowMotion();

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopDuration);

        cameraManager.Shake(shakeForce);
        Time.timeScale = slowTimeScale;

        float targetSize = originalOrthoSize * zoomSizeRatio;
        for (float t = 0f; t < slowDuration; t += Time.unscaledDeltaTime)
        {
            SetOrthoSize(Mathf.Lerp(originalOrthoSize, targetSize, Mathf.SmoothStep(0f, 1f, t / slowDuration)));
            yield return null;
        }
        SetOrthoSize(targetSize);

        for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            SetFade(t / fadeDuration);
            yield return null;
        }

        Time.timeScale = 1f;
        // Push가 게임을 멈춘다(GameFreezeManager). 결과 화면 오버레이가 페이드를 이어받는다.
        UIManager.Instance.Push(GameOverUI.Key);
        SetFade(0f);
        routine = null;
    }

    private void SetOrthoSize(float size)
    {
        var lens = virtualCamera.Lens;
        lens.OrthographicSize = size;
        virtualCamera.Lens = lens;
    }

    private void SetFade(float t)
    {
        var c = fadeColor;
        c.a = fadeColor.a * t;
        fadeImage.color = c;
        fadeImage.enabled = t > 0f;
    }
}
