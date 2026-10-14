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
        if (e.Source is Player)
        {
            Shake(hitShakeForce);
        }
        else if (e.Attacker is Player player && player.Weapon != null)
        {
            Shake(player.Weapon.shakeForce);
            SlowMotion(player.Weapon.hitStopTimeScale, player.Weapon.hitStopDuration);
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

    private IEnumerator SlowMotionRoutine(float timeScale, float duration)
    {
        Time.timeScale = timeScale;
        yield return new WaitForSecondsRealtime(duration);

        if (Mathf.Approximately(Time.timeScale, timeScale))
            Time.timeScale = 1f;
        slowMotionRoutine = null;
    }
}
