using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraManager : MonoBehaviour, IObserver<EntityDamagedEvent>
{
    [SerializeField] private float hitShakeForce = 1f;
    [SerializeField, Range(0f, 1f)] private float hitStopTimeScale = 0.1f;
    [SerializeField, Min(0)] private float hitStopDuration = 0.08f;

    private CinemachineImpulseSource impulseSource;
    private Coroutine slowMotionRoutine;

    private void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        // 슬로우 모션 중에도 흔들림 길이가 늘어나지 않도록 실제 시간 기준으로 재생
        CinemachineImpulseManager.Instance.IgnoreTimeScale = true;
    }

    private void OnEnable() => EventBus.Subscribe<EntityDamagedEvent>(this);

    private void OnDisable()
    {
        EventBus.Unsubscribe<EntityDamagedEvent>(this);
        if (slowMotionRoutine != null)
        {
            slowMotionRoutine = null;
            Time.timeScale = 1f;
        }
    }

    public void OnNotify(EntityDamagedEvent e)
    {
        if (e.Source is Player)
        {
            Shake(hitShakeForce);
        }
        else if (e.Attacker is Player player)
        {
            if (player.Weapon != null)
                Shake(player.Weapon.shakeForce);
            SlowMotion(hitStopTimeScale, hitStopDuration);
        }
    }

    // 매번 무작위 방향으로 흔든다
    public void Shake(float force) => impulseSource.GenerateImpulseWithVelocity(Random.insideUnitCircle.normalized * force);

    public void SlowMotion(float timeScale, float duration)
    {
        // GameFreezeManager가 게임을 멈춘 상태면 건드리지 않는다
        if (Time.timeScale == 0f)
            return;

        if (slowMotionRoutine != null)
            StopCoroutine(slowMotionRoutine);
        slowMotionRoutine = StartCoroutine(SlowMotionRoutine(timeScale, duration));
    }

    private IEnumerator SlowMotionRoutine(float timeScale, float duration)
    {
        Time.timeScale = timeScale;
        yield return new WaitForSecondsRealtime(duration);

        // 도중에 GameFreezeManager가 timeScale을 바꿨다면 그쪽에 맡긴다
        if (Mathf.Approximately(Time.timeScale, timeScale))
            Time.timeScale = 1f;
        slowMotionRoutine = null;
    }
}
