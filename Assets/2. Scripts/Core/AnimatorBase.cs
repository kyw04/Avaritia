using UnityEngine;
using System.Collections;

public class AnimatorBase : MonoBehaviour
{
    public Animator AnimatorComponent { get; private set; }
    protected Coroutine animCoroutine;
    
    protected virtual void Awake()
    {
        AnimatorComponent = GetComponent<Animator>();
    }
    
    protected void PlayAnimation(string animName)
    {
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }
        
        AnimatorComponent.Play(animName);
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