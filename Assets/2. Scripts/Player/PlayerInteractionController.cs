using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private InteractPromptUI prompt;
    [SerializeField] private float detectRadius = 1.5f;
    [SerializeField] private float tapThreshold = 0.15f;
    [SerializeField] private float fillStartDelay = 0.5f;
    [SerializeField] private float holdDuration = 0.6f;

    private IInteractable current;
    private Transform promptAnchor;
    private InputAction promptAction;
    private bool isHolding;
    private float pressStartTime;

    private void Update()
    {
        UpdateNearest();
        if (isHolding) UpdateHold();
    }

    private void UpdateNearest()
    {
        if (isHolding) return;

        bool hasMarkerSkill = TryFindMarkerSkill(out var marker, out var skillAction, out bool holding, out float holdProgress);
        // 키를 누르고 있는 채널링 중에는 진행도를 보여 주고 아이템 상호작용은 받지 않는다.
        current = holding ? null : WorldInteractionManager.Instance.GetNearestInRange(player.transform.position, detectRadius);

        // 그 외에는 상호작용 대상(아이템 등)이 우선이고, 없으면 근처 마커로 지금 쓸 수 있는 스킬의 키를 띄운다.
        Transform anchor = null;
        InputAction action = null;
        if (current != null)
        {
            anchor = current.Transform;
            action = InputHandler.Instance.InputAction.Gameplay.Interact;
        }
        else if (hasMarkerSkill)
        {
            anchor = marker.transform;
            action = skillAction;
        }

        if (anchor != promptAnchor || action != promptAction)
        {
            promptAnchor = anchor;
            promptAction = action;

            if (anchor == null)
            {
                prompt.Hide();
                return;
            }
            prompt.transform.position = anchor.position + Vector3.up;
            // "Hold F | Hold Triangle" 대신 키보드 키 이름만 보여 준다.
            prompt.Show(action.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions, "Keyboard&Mouse"));
        }

        if (anchor != null && current == null)
            prompt.SetProgress(holdProgress);
    }

    // 슬롯 스킬 중 NearbyMarkerCondition을 가진 스킬이 마커 범위 안에 있고, 지금 쓸 수 있거나 키를 누르고 있는 채널링 중이면
    // 그 마커와 스킬 키를 돌려준다. 누르고 있는 채널링이면 holding과 진행도도 돌려준다(한 번 누르는 채널링 중엔 띄우지 않는다).
    private bool TryFindMarkerSkill(out AbilityMarker marker, out InputAction action, out bool holding, out float progress)
    {
        marker = null;
        action = null;
        holding = false;
        progress = 0f;

        var gameplay = InputHandler.Instance.InputAction.Gameplay;
        for (int slot = 0; slot < 2; slot++)
        {
            var data = player.Abilities.AbilityAt(slot);
            if (data == null) continue;

            holding = player.Abilities.TryGetHoldChannelProgress(data, out progress);
            // 행동 제한 중(채널링, 끝 애니메이션 등)이거나 쿨다운 중이면 스킬 입력이 막혀 있으므로 띄우지 않는다.
            if (!holding && (player.ActionLock != ActionLock.Free || player.Abilities.IsOnCooldown(data) || player.Abilities.IsChanneling(data))) continue;

            foreach (var condition in data.conditions)
            {
                if (condition is not NearbyMarkerCondition near) continue;
                marker = AbilityMarker.FindNearest(near.markerId, player.transform.position, near.radius);
                if (marker == null) continue;
                action = slot == 0 ? gameplay.Skill1 : gameplay.Skill2;
                return true;
            }
        }
        marker = null;
        action = null;
        holding = false;
        progress = 0f;
        return false;
    }

    private void UpdateHold()
    {
        if (current == null) { isHolding = false; return; }

        float elapsed = Time.time - pressStartTime - fillStartDelay;
        if (elapsed < 0f) return;

        float t = elapsed / holdDuration;
        prompt.SetProgress(t);

        if (t >= 1f)
        {
            isHolding = false;
            Resolve(InteractChoice.Secondary);
        }
    }

    public void OnInteractStarted()
    {
        if (current == null) return;

        pressStartTime = Time.time;
        if (!current.NeedsChoice(player))
        {
            Resolve(InteractChoice.Primary);
            return;
        }
        isHolding = true;
    }

    public void OnInteractCanceled()
    {
        if (!isHolding) return;
        isHolding = false;
        if (current == null) return;

        float held = Time.time - pressStartTime;
        if (held <= tapThreshold) Resolve(InteractChoice.Primary);
        else prompt.SetProgress(0f);
    }

    private void Resolve(InteractChoice choice)
    {
        var target = current;
        current = null;
        promptAnchor = null;
        promptAction = null;
        prompt.Hide();
        target.Interact(player, choice);
    }
}
