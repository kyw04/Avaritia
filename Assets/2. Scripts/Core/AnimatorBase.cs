using UnityEngine;
using System.Collections;

public class AnimatorBase : MonoBehaviour
{
    protected Animator animator;
    protected Coroutine animCoroutine;
    
    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
    }
    
    protected void PlayAnimation(string animName)
    {
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }
        
        animator.Play(animName);
    }
    
    protected IEnumerator PlayAnimationAfterDelay(string animName, float length)
    {
        yield return new WaitForSeconds(length);
        PlayAnimation(animName);
    }

    private void OnDisable()
    {
        EventBus.UnsubscribeAll(this);
    }
}