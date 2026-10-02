using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : Singleton<ParticleManager>,
    IObserver<EntityJumpedEvent>,
    IObserver<EntityLandedEvent>
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
    
    public void OnNotify(EntityJumpedEvent e) => Spawn(e.Data.particle, e.Source.transform);
    public void OnNotify(EntityLandedEvent e) => Spawn(e.Particle, e.Source.transform);
}
