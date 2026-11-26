using UnityEngine;

// 엔티티가 죽을 때 떨어뜨릴 픽업. 엔티티는 사망 시 Drop만 요청하고, 무엇을 만들지는 여기서 정한다.
public class DropTable : MonoBehaviour
{
    [SerializeField] private WeaponData dropWeaponAsset;
    [SerializeField] private AbilityData dropAbilityAsset;
    [SerializeField, Range(0, 100)] private float dropChance;

    public void Drop(Vector3 position)
    {
        IInteractable payload = null;
        if (dropWeaponAsset != null)
            payload = new WeaponPickup(dropWeaponAsset);
        else if (dropAbilityAsset != null)
            payload = dropAbilityAsset is Item item ? new ItemPickup(item) : new AbilityPickup(dropAbilityAsset);

        if (payload == null) return;
        if (Random.Range(0f, 100f) >= dropChance) return;

        WorldInteractionManager.Instance.Spawn(payload, position);
    }
}
