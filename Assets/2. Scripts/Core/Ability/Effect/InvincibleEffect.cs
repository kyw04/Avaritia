[System.Serializable]
public class InvincibleEffect : IAbilityEffect
{
    public float duration;

    public void Apply(AbilityContext context)
    {
        if (context.Caster is Entity entity)
            entity.SetInvincible(duration);
    }
}
