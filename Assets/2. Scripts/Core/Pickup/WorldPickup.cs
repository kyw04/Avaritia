using UnityEngine;

public class WorldPickup : MonoBehaviour
{
    [SerializeField] private Weapon weaponAsset;
    [SerializeField] private SkillData skillAsset;
    [SerializeField] private PickupPromptUI prompt;

    public IPickupable Payload { get; private set; }
    public PickupPromptUI Prompt => prompt;

    private void Awake()
    {
        if (weaponAsset != null) Payload = new WeaponPickup(weaponAsset);
        else if (skillAsset != null) Payload = new SkillPickup(skillAsset);

        WorldPickupManager.Instance.Register(this);
    }

    private void OnDestroy()
    {
        WorldPickupManager.Instance.Unregister(this);
    }

    public void Init(IPickupable payload) => Payload = payload;
}
