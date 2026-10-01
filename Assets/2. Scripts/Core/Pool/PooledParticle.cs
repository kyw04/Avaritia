using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PooledParticle : MonoBehaviour, IPoolable
{
    public bool persistent;
    public Vector3 position;
    public Quaternion rotation;
    
    private ParticleSystem ps;

    public bool IsRunning { get; private set; }
    public float LastUsedTime { get; private set; }
    
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
        LastUsedTime = Time.time;
        if (!ParticleManager.usedParticlesList.Contains(this))
            ParticleManager.usedParticlesList.Add(this);
    }
}