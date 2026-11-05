using UnityEngine;

// 던져진 오브젝트를 중력으로 떨어뜨리다가 Floor/Platform 위에 닿으면 그 자리에 멈춘다.
// 트리거 콜라이더만 있어도 바닥을 지나치지 않도록 매 물리 프레임 아래로 레이를 쏴서 착지를 판정한다.
[RequireComponent(typeof(Rigidbody2D))]
public class ThrowLanding : MonoBehaviour
{
    private Rigidbody2D rb;
    private bool flying;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    public void Launch(Vector2 velocity)
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = velocity;
        flying = true;
    }

    private void FixedUpdate()
    {
        if (!flying || rb.linearVelocityY > 0f)
            return;

        // 바닥(콜라이더 아래 끝)까지의 거리에 이번 프레임 낙하량을 더해 그 안에 바닥이 있으면 착지시킨다.
        float bottom = BottomOffset();
        float distance = bottom + -rb.linearVelocityY * Time.fixedDeltaTime;
        var hit = Physics2D.Raycast(rb.position, Vector2.down, distance, LayerMask.GetMask("Floor", "Platform"));
        if (!hit)
            return;

        rb.position = new Vector2(rb.position.x, hit.point.y + bottom);
        Stop();
    }

    private float BottomOffset()
    {
        float offset = 0f;
        foreach (var col in GetComponentsInChildren<Collider2D>())
            offset = Mathf.Max(offset, rb.position.y - col.bounds.min.y);
        return offset;
    }

    private void Stop()
    {
        flying = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    // 풀은 IPoolable을 하나만 호출하므로, 날아가다 반납돼도 다음에 멈춘 상태로 나오도록 비활성화될 때 멈춘다.
    private void OnDisable() => Stop();
}
