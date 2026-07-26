using UnityEngine;

public class Boss : Entity
{
    public IAIController Machine { get; private set; }
    public Transform Target { get; private set; }
    private StateManager stateManager;

    // Machine is built here too (not just OnSpawn) so a directly scene-placed
    // (non-pooled) instance still gets a working state machine — OnSpawn only
    // fires for pool-spawned instances. OnSpawn unregisters and rebuilds it
    // again on every pool (re)spawn.
    protected override void Awake()
    {
        base.Awake();
        stateManager = StateManager.Instance;

        var player = FindAnyObjectByType<Player>();
        if (player != null)
            Target = player.transform;

        Machine = new BossBehaviorTree(this);
        Machine.Init();
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        if (Machine != null) stateManager.Unregister(Machine);
        var player = FindAnyObjectByType<Player>();
        if (player != null)
            Target = player.transform;

        Machine = new BossBehaviorTree(this);
        Machine.Init();
    }

    public void MoveToTarget()
    {
        if (Target == null)
            return;

        int flip = FlipToTarget();
        if (Mathf.Abs(Target.position.x) - Mathf.Abs(transform.position.x) <= 0.5f)
            return;

        Move(new Vector2(flip, 0));
    }

    public int FlipToTarget()
    {
        int flip = Target.position.x > transform.position.x ? 1 : -1;
        float scale = Mathf.Abs(transform.localScale.x);
        transform.localScale = new Vector3(scale * flip, scale, scale);
        return flip;
    }

    public override void Die()
    {
        if (!TryMarkDead()) return;
        stateManager.Unregister(Machine);
        ObjectPoolManager.Instance.Despawn(gameObject);
        Debug.Log("Boss: 사망");
    }

    private void OnDestroy()
    {
        stateManager.Unregister(Machine);
    }
}
