using UnityEngine;

public interface IAttacker
{
    MonoBehaviour Mono { get; }
    float Damage { get; }
    bool IsAttacking { get; set; }
}