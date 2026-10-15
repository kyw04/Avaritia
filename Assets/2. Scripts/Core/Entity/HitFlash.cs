using System.Collections;
using UnityEngine;

// 이 엔티티가 피격되면 자식 스프라이트들을 잠깐 flashMaterial로 바꿨다가 되돌린다.
[RequireComponent(typeof(Entity))]
public class HitFlash : MonoBehaviour, IObserver<EntityDamagedEvent>
{
    [SerializeField] private Material flashMaterial;
    [SerializeField, Min(0)] private float duration = 0.1f;

    private Entity entity;
    private SpriteRenderer[] renderers;
    private Material[] originalMaterials;
    private Coroutine flashRoutine;

    private void Awake()
    {
        entity = GetComponent<Entity>();
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalMaterials[i] = renderers[i].sharedMaterial;
    }

    private void OnEnable() => EventBus.Subscribe<EntityDamagedEvent>(this);

    // 플래시 도중 비활성화(풀 반환 등)되면 코루틴이 멈추므로 여기서 원래 머티리얼로 되돌린다.
    private void OnDisable()
    {
        EventBus.Unsubscribe<EntityDamagedEvent>(this);
        flashRoutine = null;
        Restore();
    }

    public void OnNotify(EntityDamagedEvent e)
    {
        if (e.Source != entity || flashMaterial == null) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        foreach (var r in renderers)
            r.sharedMaterial = flashMaterial;
        yield return new WaitForSeconds(duration);
        Restore();
        flashRoutine = null;
    }

    private void Restore()
    {
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sharedMaterial = originalMaterials[i];
    }
}
