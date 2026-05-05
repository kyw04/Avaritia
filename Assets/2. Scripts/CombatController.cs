public class CombatController : IAttacker, IDamageable
{
    
    
    public void Attack(IDamageable target)
    {
        target.TakeDamage(1);
    }

    public void TakeDamage(float damage)
    {
        
    }
}