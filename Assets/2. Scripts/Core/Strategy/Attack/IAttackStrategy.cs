using UnityEngine;

public interface IAttackStrategy
{
    void Offensive(IAttacker attacker, float damageMultiplier, ContactFilter2D filter, Transform target = null);
}
