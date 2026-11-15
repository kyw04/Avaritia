using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemBox : MonoBehaviour, IInteractable, IPoolable
{
    [SerializeField] private Animator animator;
    [SerializeField] private float popUpSpeed = 5f;
    [SerializeField] private float popHorizontalSpeed = 2f;
    [SerializeField] private float itemSpawnDelayMax = 0.25f;
    
    [Header("Test Case")]
    [SerializeField] private List<ScriptableObject> itemPool;
    [SerializeField] private int itemCount;
    private bool opened;
    private bool itemsSpawned;
    private WorldInteractionManager manager;

    public string DisplayName => "상자";
    public Sprite Icon => null;
    public string Details => null;
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

    public void Init(List<ScriptableObject> pool, int count)
    {
        itemPool = pool;
        itemCount = count;
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
        StartCoroutine(StartSpawnItems());
    }

    private IEnumerator StartSpawnItems()
    {
        if (itemPool == null) yield break;

        var pool = new List<ScriptableObject>(itemPool);
        int count = Mathf.Min(itemCount, pool.Count);
        var spawned = new List<WorldPickup>();

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);
            var asset = pool[index];
            pool.RemoveAt(index);

            var payload = BuildPayload(asset);
            if (payload == null) continue;

            var pickup = WorldInteractionManager.Instance.Spawn(payload, transform.position);
            spawned.Add(pickup);
        }

        if (spawned.Count > 1)
        {
            foreach (var pickup in spawned)
                pickup.SetBatchGroup(spawned);
        }
        
        for (int i = 0; i < spawned.Count; i++)
        {
            yield return new WaitForSeconds(Random.Range(0.1f, itemSpawnDelayMax));
            float direction = i == 0 ? 0 : i % 2 == 0 ? 1f : -1f;
            float horizontalSpeed = i % 2 == 0 ? popHorizontalSpeed * i / 2f : popHorizontalSpeed * i;
            float verticalSpeed = Random.Range(popUpSpeed * 0.8f, popUpSpeed);
            spawned[i].Launch(new Vector2(direction * horizontalSpeed, verticalSpeed));
        }
    }

    private IInteractable BuildPayload(ScriptableObject asset)
    {
        if (asset is WeaponData data) return new WeaponPickup(data);
        // Item은 AbilityData를 상속하므로 먼저 검사해야 스킬 픽업으로 잘못 만들어지지 않는다.
        if (asset is Item item) return new ItemPickup(item);
        if (asset is AbilityData ability) return new AbilityPickup(ability);
        return null;
    }
}
