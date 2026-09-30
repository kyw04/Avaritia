using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerArmAnimator : MonoBehaviour
{
    [SerializeField] private EntityAnimator playerMainAnimator;
    
    private Animator mainAnimator;
    private Animator armAnimator;

    private void Start()
    {
        if (playerMainAnimator == null)
        {
            Debug.LogError("PlayerArmAnimator component is missing");
            gameObject.SetActive(false);
            return;
        }
        
        armAnimator = GetComponent<Animator>();
        mainAnimator = playerMainAnimator.AnimatorComponent;
        
        
    }

    private void LateUpdate()
    {
        if (playerMainAnimator == null)
            return;

        AnimatorClipInfo[] clips = mainAnimator.GetCurrentAnimatorClipInfo(0);
        AnimationClip dominant = null;
        float maxWeight = 0f;

        foreach (var info in clips)
        {
            if (info.weight > maxWeight)
            {
                maxWeight = info.weight;
                dominant = info.clip;
            }
        }

        if (dominant != null)
            armAnimator.Play(dominant.name);
    }
}