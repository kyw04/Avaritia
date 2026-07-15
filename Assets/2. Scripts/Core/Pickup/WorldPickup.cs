using UnityEngine;

public class WorldPickup : MonoBehaviour
{
    [SerializeField] private Weapon weaponAsset;
    [SerializeField] private SkillData skillAsset;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public IPickupable Payload { get; private set; }

    private WorldPickupManager manager;

    private void Awake()
    {
        if (weaponAsset != null) Payload = new WeaponPickup(weaponAsset);
        else if (skillAsset != null) Payload = new SkillPickup(skillAsset);

        ApplyIcon();
        manager = WorldPickupManager.Instance;
        manager.Register(this);
    }

    private void OnDestroy()
    {
        manager.Unregister(this);
    }

    public void Init(IPickupable payload)
    {
        Payload = payload;
        ApplyIcon();
    }

    private void ApplyIcon()
    {
        if (Payload?.Icon != null) spriteRenderer.sprite = Payload.Icon;
    }
}
