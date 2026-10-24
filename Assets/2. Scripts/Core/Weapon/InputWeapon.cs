using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 장착 중에만 PlayerInputActions의 Gameplay 입력(started/performed/canceled)을 받아 OnInput으로 넘겨주는 무기.
// 어떤 액션인지는 context.action == Gameplay.Move 처럼 비교해 구분한다.
public abstract class InputWeapon : Weapon
{
    [System.Serializable]
    public class InputAnimation
    {
        public InputActionReference action;
        public InputActionPhase phase = InputActionPhase.Performed;
        [Tooltip("비워두면 액션의 모든 키에 반응한다. 특정 키만: downArrow, s, space 등 (컨트롤 이름)")]
        public string controlName;
        public AnimationClip clip;
    }

    [SerializeField] private List<InputAnimation> inputAnimations = new();

    protected PlayerInputActions.GameplayActions Gameplay => InputHandler.Instance.InputAction.Gameplay;

    public override void Equip(Entity owner, ImageCountBar countBar = null)
    {
        base.Equip(owner, countBar);
        Gameplay.Get().actionTriggered += HandleInput;
    }

    public override void Unequip()
    {
        base.Unequip();
        Gameplay.Get().actionTriggered -= HandleInput;
    }

    private void HandleInput(InputAction.CallbackContext context)
    {
        PlayInputAnimation(context);
        OnInput(context);
    }

    // 에셋의 액션(InputActionReference)과 생성된 PlayerInputActions의 액션은 인스턴스가 달라 id로 비교한다.
    private void PlayInputAnimation(InputAction.CallbackContext context)
    {
        foreach (var entry in inputAnimations)
        {
            if (entry.clip == null || entry.action == null || entry.action.action.id != context.action.id)
                continue;
            if (entry.phase != context.phase)
                continue;
            if (!string.IsNullOrEmpty(entry.controlName) && context.control?.name != entry.controlName)
                continue;

            EventBus.Publish(new EntityAnimationEvent(Owner, entry.clip));
            return;
        }
    }

    protected abstract void OnInput(InputAction.CallbackContext context);
}
