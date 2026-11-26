using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimatorBase : MonoBehaviour
{
    public Animator AnimatorComponent { get; private set; }
    protected Coroutine animCoroutine;
    // 파생 클래스가 나중에 재생하려고 기다리는 애니메이션이 있는지. 다른 애니메이션을 재생하면 취소된다.
    protected bool hasPendingAnimation;

    protected virtual void Awake()
    {
        AnimatorComponent = GetComponent<Animator>();
    }
    
    protected void PlayAnimation(string animName)
    {
        hasPendingAnimation = false;
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }
        
        AnimatorComponent.Play(animName);
    }

    protected void PlayAnimation(int stateHash)
    {
        hasPendingAnimation = false;
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }

        AnimatorComponent.Play(stateHash);
    }

    // 클립 이름과 같은 이름의 상태 해시. clip.name은 읽을 때마다 문자열을 새로 만들어 클립별로 한 번만 계산한다.
    private static readonly Dictionary<AnimationClip, int> clipHashes = new();

    protected static int ClipHash(AnimationClip clip)
    {
        if (!clipHashes.TryGetValue(clip, out int hash))
        {
            hash = Animator.StringToHash(clip.name);
            clipHashes[clip] = hash;
        }
        return hash;
    }

    protected IEnumerator PlayAnimationAfterDelay(string animName, float length)
    {
        yield return new WaitForSeconds(length);
        PlayAnimation(animName);
    }

    protected virtual void OnDisable()
    {
        EventBus.UnsubscribeAll(this);
    }
}