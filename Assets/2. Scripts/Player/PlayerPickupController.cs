using UnityEngine;

public class PlayerPickupController : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private PickupPromptUI prompt;
    [SerializeField] private float detectRadius = 1.5f;
    [SerializeField] private float tapThreshold = 0.15f;
    [SerializeField] private float fillStartDelay = 0.5f;
    [SerializeField] private float holdDuration = 0.6f;

    private WorldPickup current;
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

        var nearest = WorldPickupManager.Instance.GetNearestInRange(player.transform.position, detectRadius);
        if (nearest == current) return;

        current = nearest;
        if (current != null)
        {
            prompt.transform.position = current.transform.position + Vector3.up;
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
            Resolve(PickupChoice.Secondary);
        }
    }

    public void OnInteractStarted()
    {
        if (current == null) return;

        pressStartTime = Time.time;
        if (!current.Payload.NeedsChoice(player))
        {
            Resolve(PickupChoice.Primary);
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
        if (held <= tapThreshold) Resolve(PickupChoice.Primary);
        else prompt.SetProgress(0f);
    }

    private void Resolve(PickupChoice choice)
    {
        var target = current;
        target.Payload.Pickup(player, choice, player.transform.position);
        current = null;
        prompt.Hide();
        Destroy(target.gameObject);
    }
}
