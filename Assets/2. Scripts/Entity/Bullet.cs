using UnityEngine;

public class Bullet : MonoBehaviour, IAttacker
{
    public MonoBehaviour Mono { get; }
    public float Damage { get; }
    public bool IsAttacking { get; set; }
    public int LookDirection => 1;
}
