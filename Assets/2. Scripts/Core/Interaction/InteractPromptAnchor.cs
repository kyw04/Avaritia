using UnityEngine;

// 상호작용 키 표시를 이 오브젝트 기준 어디에 띄울지 정한다.
// 없으면 PlayerInteractionController의 기본 위치(defaultPromptOffset)를 쓴다.
public class InteractPromptAnchor : MonoBehaviour
{
    [SerializeField] private Vector2 offset = new(0f, 1f);

    public Vector3 PromptPosition => transform.position + (Vector3)offset;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(PromptPosition, new Vector3(0.3f, 0.3f, 0f));
    }
}
