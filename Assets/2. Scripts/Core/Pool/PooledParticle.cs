using System;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PooledParticle : MonoBehaviour, IPoolable
{
    public bool persistent;
    public Vector3 position;
    public Quaternion rotation;
    
    private ParticleSystem ps;
    private Transform followTarget;
    private float followUntil;
    private Func<bool> keepFollowing;

    public bool IsRunning { get; private set; }
    public float LastUsedTime { get; private set; }
    public Transform FollowTarget => followTarget;
    
    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();

        var psRenderer = GetComponent<ParticleSystemRenderer>();
        psRenderer.sortingLayerName = "Effect";
        psRenderer.sortingOrder = 20;
        foreach (var r in GetComponentsInChildren<ParticleSystemRenderer>())
        {
            r.sortingLayerName = "Effect";
            r.sortingOrder = 20;
        }
        
        var main = ps.main;
        main.stopAction = ParticleSystemStopAction.Callback;
    }

    // 대상 위치(+ position 오프셋)를 따라다닌다. duration이 양수면 그 시간이 지나면 방출을 멈추고,
    // 0 이하면 파티클 자체 길이만큼 재생된다. 대상이 비활성화(사망/풀 반납)되거나 keepAlive가 false가 되면 바로 멈춘다.
    public void Follow(Transform target, float duration, Func<bool> keepAlive = null)
    {
        followTarget = target;
        keepFollowing = keepAlive;
        followUntil = duration > 0f ? Time.time + duration : float.MaxValue;
        transform.position = target.position + position;
    }

    // 같은 대상에 다시 걸렸을 때 새로 띄우지 않고 남은 시간만 늘린다.
    public void ExtendFollow(float duration)
    {
        followUntil = duration > 0f ? Mathf.Max(followUntil, Time.time + duration) : float.MaxValue;
    }

    private void LateUpdate()
    {
        if (followTarget == null) return;

        if (!followTarget.gameObject.activeInHierarchy || followUntil <= Time.time
            || (keepFollowing != null && !keepFollowing()))
        {
            followTarget = null;
            keepFollowing = null;
            // 이미 방출된 파티클은 자연스럽게 사라지게 두고, 다 사라지면 OnParticleSystemStopped에서 반납된다.
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            return;
        }

        transform.position = followTarget.position + position;
    }

    private void OnParticleSystemStopped()
    {
        ObjectPoolManager.Instance.Despawn(this);
    }
    
    public void OnSpawn()
    {
        ps.Play();
        IsRunning = true;
    }

    public void OnDespawn()
    {
        IsRunning = false;
        followTarget = null;
        keepFollowing = null;
        LastUsedTime = Time.time;
        if (!ParticleManager.usedParticlesList.Contains(this))
            ParticleManager.usedParticlesList.Add(this);
    }
}