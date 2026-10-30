using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : Singleton<ParticleManager>,
    IObserver<EntityJumpedEvent>,
    IObserver<EntityLandedEvent>,
    IObserver<EntityTurnEvent>,
    IObserver<EntityDashEvent>,
    IObserver<EntityDamagedEvent>,
    IObserver<EntityAttackStartEvent>
{
    public static readonly List<PooledParticle> usedParticlesList = new();

    [SerializeField] private Dictionary<PooledParticle, int> prewarmParticles = new(); // Dictionary<파티클 프리펩, 개수>
    [SerializeField, Min(0)] private float idleTimeout = 30f;
    [SerializeField, Min(0)] private float trimInterval = 5f;
    
    private ObjectPoolManager objectPoolManager;
    private float nextTrimTime;
    // (부여한 쪽, 대상)별로 따라다니는 파티클. 같은 버프가 다시 걸리면 겹쳐 띄우지 않고 시간만 늘린다.
    private readonly Dictionary<(object, Transform), PooledParticle> followParticles = new();
    
    // 도메인 리로드가 꺼져 있어 static 리스트가 Play 세션 사이에 남으므로 매 Play 시작 시 비운다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => usedParticlesList.Clear();

    private void Start()
    {
        EventBus.Subscribe<EntityJumpedEvent>(this);
        EventBus.Subscribe<EntityLandedEvent>(this);
        EventBus.Subscribe<EntityTurnEvent>(this);
        EventBus.Subscribe<EntityDashEvent>(this);
        EventBus.Subscribe<EntityDamagedEvent>(this);
        EventBus.Subscribe<EntityAttackStartEvent>(this);
        
        objectPoolManager = ObjectPoolManager.Instance;
        foreach (var p in prewarmParticles)
        {
            var obj = objectPoolManager.Spawn(p.Key.gameObject, Vector3.zero, Quaternion.identity);
            objectPoolManager.Despawn(obj);
        }
        
        nextTrimTime = trimInterval;
    }

    private void Update()
    {
        if (Time.time < nextTrimTime)
            return;
        
        nextTrimTime = Time.time + trimInterval;
        var removeBuffer = new List<PooledParticle>();
        var destroyBuffer = new List<GameObject>();
        foreach (var p in usedParticlesList)
        {
            if (p == null || p.IsRunning)
            {
                removeBuffer.Add(p);
                continue;
            }

            if (!p.persistent && Time.time - p.LastUsedTime > idleTimeout)
            {
                destroyBuffer.Add(p.gameObject);
                removeBuffer.Add(p);
            }
        }

        foreach (var remove in removeBuffer)
            usedParticlesList.Remove(remove);
        foreach (var destroy in destroyBuffer)
            objectPoolManager.Remove(destroy);

        var finishedFollows = new List<(object, Transform)>();
        foreach (var pair in followParticles)
            if (pair.Value == null || !pair.Value.IsRunning || pair.Value.FollowTarget != pair.Key.Item2)
                finishedFollows.Add(pair.Key);
        foreach (var key in finishedFollows)
            followParticles.Remove(key);
    }

    public PooledParticle Spawn(PooledParticle particle, Transform target = null)
    {
        if (particle == null)
            return null;
        
        var pos = particle.position;
        if (target != null) pos += target.position;
        
        return objectPoolManager.Spawn(particle, pos, particle.rotation);
    }
    // 대상을 따라다니는 파티클을 띄운다. key가 있으면 같은 key/대상으로 이미 따라다니는 파티클의 시간만 늘린다.
    // keepAlive를 주면 false가 되는 순간 멈춘다(예: 실드가 깨질 때).
    public PooledParticle SpawnFollow(PooledParticle particle, Transform target, float duration, object key = null,
        System.Func<bool> keepAlive = null)
    {
        if (particle == null || target == null)
            return null;

        if (key != null && followParticles.TryGetValue((key, target), out var existing)
            && existing != null && existing.IsRunning && existing.FollowTarget == target)
        {
            existing.ExtendFollow(duration);
            return existing;
        }

        var spawned = objectPoolManager.Spawn(particle, target.position + particle.position, particle.rotation);
        spawned.Follow(target, duration, keepAlive);
        if (key != null)
            followParticles[(key, target)] = spawned;
        return spawned;
    }

    public PooledParticle Spawn(PooledParticle particle, Vector3 position = default, Quaternion rotation = default)
    {
        if (particle == null)
            return null;
        
        var pos = particle.position + position;
        var rot = particle.rotation * rotation;
        return objectPoolManager.Spawn(particle, pos, rot);
    }
    
    public void OnNotify(EntityJumpedEvent e) => Spawn(e.Data.jump.particle, e.Source.transform);
    public void OnNotify(EntityLandedEvent e) => Spawn(e.Particle, e.Source.transform);
    public void OnNotify(EntityTurnEvent e) => Spawn(e.Particle, e.Source.transform);
    public void OnNotify(EntityDashEvent e) => Spawn(e.Particle, e.Source.transform);
    public void OnNotify(EntityDamagedEvent e)
    {
        if (e.Attacker == null)
        {
            Spawn(e.Source.Feedbacks.Hit.particle, e.Source.transform.position);
            return;
        }

        Vector3 from = e.HitPoint ?? e.Attacker.transform.position;
        Vector3 mid = (e.Source.transform.position + from) * 0.5f;
        Spawn(e.Source.Feedbacks.Hit.particle, mid);
        if (e.Attacker is Player player && player.Weapon != null)
            Spawn(player.Weapon.hit.particle, mid);
    }

    // SoundManager와 같은 이유로 무기 콤보 공격일 때만 스윙 파티클을 띄운다.
    public void OnNotify(EntityAttackStartEvent e)
    {
        if (e.Source is Player player && player.Weapon != null && player.Weapon.combo != null
            && player.Weapon.combo.datas.Contains(e.Data))
            Spawn(player.Weapon.swing.particle, e.Source.transform);
    }
}
