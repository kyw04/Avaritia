using System;
using UnityEngine;

// 한 동작(공격 준비, 점프, 피격 등)에 함께 재생할 애니메이션, 파티클, 소리.
// 애니메이션은 Animator를 가진 쪽에서 재생하고, Play는 파티클과 소리만 재생한다.
[Serializable]
public struct ActionFeedback
{
    public AnimationClip animClip;
    public PooledParticle particle;
    public AudioClip sound;

    public void Play(Vector3 position) => Play(position, Quaternion.identity);

    // 방향이 있는 파티클은 rotation으로 돌려 띄운다. 띄운 파티클을 돌려줘 따라다니게 할 수 있다(없으면 null).
    public PooledParticle Play(Vector3 position, Quaternion rotation)
    {
        PooledParticle spawned = null;
        if (particle != null)
            spawned = ObjectPoolManager.Instance.Spawn(particle, position, rotation);
        if (sound != null)
            SoundManager.Instance.PlayOneShotClip(sound, AudioType.Effect);
        return spawned;
    }
}
