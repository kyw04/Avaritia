using UnityEngine;

public class WorldPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private Weapon weaponAsset;
    [SerializeField] private SkillData skillAsset;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private IInteractable payload;

    public string DisplayName => payload.DisplayName;
    public Sprite Icon => payload.Icon;
    public Transform Transform => transform;

    private void Awake()
    {
        if (weaponAsset != null) payload = new WeaponPickup(weaponAsset);
        else if (skillAsset != null) payload = new SkillPickup(skillAsset);

        ApplyIcon();
        WorldInteractionManager.Instance.Register(this);
    }

    private void OnDestroy()
    {
        WorldInteractionManager.Instance.Unregister(this);
    }

    public void Init(IInteractable payload)
    {
        this.payload = payload;
        ApplyIcon();
    }

    public bool NeedsChoice(Player player) => payload.NeedsChoice(player);

    public void Interact(Player player, InteractChoice choice)
    {
        payload.Interact(player, choice);
        Destroy(gameObject);
    }

    private void ApplyIcon()
    {
        if (payload?.Icon != null) spriteRenderer.sprite = payload.Icon;
    }
}
