using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public struct HitCameraSettings
{
    public float shakeForce;
    [Range(0f, 1f)] public float hitStopTimeScale;
    [Min(0)] public float hitStopDuration;

    public static HitCameraSettings Default => new HitCameraSettings
    {
        shakeForce = 0.25f,
        hitStopTimeScale = 0.1f,
        hitStopDuration = 0.05f
    };
}

[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraManager : MonoBehaviour, IObserver<EntityDamagedEvent>
{
    [SerializeField] private float hitShakeForce = 1f;

    private CinemachineImpulseSource impulseSource;
    private Coroutine slowMotionRoutine;

    private void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
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
        if (e.Info.Has(DamageFlags.NoHitReaction)) return;
        if (e.Source is Player)
        {
            Shake(hitShakeForce);
        }
        // 사망 연출 중에 날아가던 탄환 등이 맞아도 히트스톱이 연출의 슬로모션을 끊지 않게 한다.
        else if (e.Attacker is Player { IsDead: false })
        {
            Shake(e.HitCamera.shakeForce);
            SlowMotion(e.HitCamera.hitStopTimeScale, e.HitCamera.hitStopDuration);
        }
    }

    public void Shake(float force) => impulseSource.GenerateImpulseWithVelocity(Random.insideUnitCircle.normalized * force);

    public void SlowMotion(float timeScale, float duration)
    {
        if (Time.timeScale == 0f)
            return;

        if (slowMotionRoutine != null)
            StopCoroutine(slowMotionRoutine);
        slowMotionRoutine = StartCoroutine(SlowMotionRoutine(timeScale, duration));
    }

    // 진행 중인 슬로모션이 끝나면서 timeScale을 1로 되돌리지 않게 멈춘다. timeScale은 건드리지 않는다.
    public void StopSlowMotion()
    {
        if (slowMotionRoutine == null)
            return;

        StopCoroutine(slowMotionRoutine);
        slowMotionRoutine = null;
    }

    private IEnumerator SlowMotionRoutine(float timeScale, float duration)
    {
        Time.timeScale = timeScale;
        yield return new WaitForSecondsRealtime(duration);

        if (Mathf.Approximately(Time.timeScale, timeScale))
            Time.timeScale = 1f;
        slowMotionRoutine = null;
    }
}
