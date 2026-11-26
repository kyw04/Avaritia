using UnityEngine;

public class Bullet : MonoBehaviour, IAttacker
{
    public MonoBehaviour Mono { get; }
    public float Damage { get; }
    public bool IsAttacking { get; set; }
    public int LookDirection => 1;
    public int AttackCancelVersion => 0;
    public float AttackSpeedMultiplier => 1f;
    public LayerMask ResolveTargetLayer(LayerMask mask) => mask;
    public float GetDamageAgainst(Transform target) => Damage;
    public Entity SourceEntity => null;
}
