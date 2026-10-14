using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : Singleton<ParticleManager>,
    IObserver<EntityJumpedEvent>,
    IObserver<EntityLandedEvent>,
    IObserver<EntityTurnEvent>,
    IObserver<EntityDashEvent>,
    IObserver<EntityDamagedEvent>
{
    public static readonly List<PooledParticle> usedParticlesList = new();

    [SerializeField] private Dictionary<PooledParticle, int> prewarmParticles = new(); // Dictionary<파티클 프리펩, 개수>
    [SerializeField, Min(0)] private float idleTimeout = 30f;
    [SerializeField, Min(0)] private float trimInterval = 5f;
    
    private ObjectPoolManager objectPoolManager;
    private float nextTrimTime;
    
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
    }

    public PooledParticle Spawn(PooledParticle particle, Transform target = null)
    {
        if (particle == null)
            return null;
        
        var pos = particle.position;
        if (target != null) pos += target.position;
        
        return objectPoolManager.Spawn(particle, pos, particle.rotation);
    }
    public PooledParticle Spawn(PooledParticle particle, Vector3 position = default, Quaternion rotation = default)
    {
        if (particle == null)
            return null;
        
        var pos = particle.position + position;
        var rot = particle.rotation * rotation;
        return objectPoolManager.Spawn(particle, pos, rot);
    }
    
    public void OnNotify(EntityJumpedEvent e) => Spawn(e.Data.particle, e.Source.transform);
    public void OnNotify(EntityLandedEvent e) => Spawn(e.Particle, e.Source.transform);
    public void OnNotify(EntityTurnEvent e) => Spawn(e.Particle, e.Source.transform);
    public void OnNotify(EntityDashEvent e) => Spawn(e.Particle, e.Source.transform);
    public void OnNotify(EntityDamagedEvent e)
    {
        // 탄환/효과 데미지는 Attacker가 null이므로 피격 파티클만 피격자 위치에 재생
        if (e.Attacker == null)
        {
            Spawn(e.Source.Particles.Hit, e.Source.transform.position);
            return;
        }

        Vector3 mid = (e.Source.transform.position + e.Attacker.transform.position) * 0.5f;
        Spawn(e.Source.Particles.Hit, mid);
        if (e.Attacker is Player player && player.Weapon != null)
            Spawn(player.Weapon.attackParticle, mid);
    }
}
