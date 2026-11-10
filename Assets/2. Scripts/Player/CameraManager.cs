using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

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
        else if (e.Attacker is Player player && player.Weapon != null)
        {
            Shake(player.Weapon.Data.shakeForce);
            SlowMotion(player.Weapon.Data.hitStopTimeScale, player.Weapon.Data.hitStopDuration);
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
