using System;

// 디버프가 걸려 있는 동안 대상을 따라다니는 파티클을 띄운다. 디버프가 끝나면(해제 포함) 스스로 멈춘다.
[Serializable]
public class VisualModule : DebuffModule
{
    public PooledParticle particle;

    public override void OnApply(ActiveDebuff debuff)
    {
        var target = debuff.Target;
        ParticleManager.Instance.SpawnFollow(particle, target.transform, 0f, debuff,
            () => target.Debuffs.Get(debuff.Data) == debuff);
    }
}
