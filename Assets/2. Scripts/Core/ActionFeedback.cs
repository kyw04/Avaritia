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

    public void Play(Vector3 position)
    {
        if (particle != null)
            ObjectPoolManager.Instance.Spawn(particle, position, Quaternion.identity);
        if (sound != null)
            SoundManager.Instance.PlayOneShotClip(sound, AudioType.Effect);
    }
}
