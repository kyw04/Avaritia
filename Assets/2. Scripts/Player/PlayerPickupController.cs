using UnityEngine;

public class PlayerPickupController : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private PickupPromptUI prompt;
    [SerializeField] private float detectRadius = 1.5f;
    [SerializeField] private float tapThreshold = 0.15f;
    [SerializeField] private float fillStartDelay = 0.5f;
    [SerializeField] private float holdDuration = 0.6f;

    private IInteractable current;
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

        var nearest = WorldInteractionManager.Instance.GetNearestInRange(player.transform.position, detectRadius);
        if (nearest == current) return;

        current = nearest;
        if (current != null)
        {
            prompt.transform.position = current.Transform.position + Vector3.up;
            prompt.Show();
        }
        else
        {
            prompt.Hide();
        }
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
        prompt.Hide();
        target.Interact(player, choice);
    }
}
