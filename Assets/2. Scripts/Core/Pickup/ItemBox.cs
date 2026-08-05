using System.Collections.Generic;
using UnityEngine;

public class ItemBox : MonoBehaviour, IInteractable, IPoolable
{
    [SerializeField] private ItemBoxData data;
    [SerializeField] private Animator animator;
    [SerializeField] private float scatterRadius = 0.75f;

    private bool opened;
    private bool itemsSpawned;
    private WorldInteractionManager manager;

    public string DisplayName => "상자";
    public Sprite Icon => null;
    public Transform Transform => transform;

    private void Awake()
    {
        manager = WorldInteractionManager.Instance;
        manager.Register(this);
    }

    private void OnDestroy() => manager.Unregister(this);

    public void OnSpawn()
    {
        opened = false;
        itemsSpawned = false;
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
        manager.Unregister(this);
        manager.Register(this);
    }

    public void OnDespawn() => manager.Unregister(this);

    public void Init(ItemBoxData boxData)
    {
        data = boxData;
        opened = false;
        itemsSpawned = false;
    }

    public bool NeedsChoice(Player player) => false;

    public void Interact(Player player, InteractChoice choice)
    {
        if (opened) return;
        opened = true;
        manager.Unregister(this);

        if (animator != null)
            animator.SetTrigger("Open");
        else
            OnOpenAnimationComplete();
    }

    // Animation Event에서 호출된다 (오픈 애니메이션 클립 마지막 프레임에 이벤트 등록 필요).
    public void OnOpenAnimationComplete()
    {
        if (itemsSpawned) return;
        itemsSpawned = true;
        SpawnItems();
    }

    private void SpawnItems()
    {
        if (data == null) return;

        var pool = new List<ScriptableObject>(data.itemPool);
        int count = Mathf.Min(data.itemCount, pool.Count);

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);
            var asset = pool[index];
            pool.RemoveAt(index);

            var payload = BuildPayload(asset);
            if (payload == null) continue;

            Vector2 offset = Random.insideUnitCircle * scatterRadius;
            WorldInteractionManager.Instance.Spawn(payload, transform.position + (Vector3)offset);
        }
    }

    private IInteractable BuildPayload(ScriptableObject asset)
    {
        if (asset is Weapon weapon) return new WeaponPickup(weapon);
        if (asset is SkillData skill) return new SkillPickup(skill);
        return null;
    }
}
