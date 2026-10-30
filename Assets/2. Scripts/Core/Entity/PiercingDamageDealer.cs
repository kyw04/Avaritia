using System.Collections.Generic;
using UnityEngine;

// 맞혀도 사라지지 않고 경로의 적을 관통한다. 같은 대상은 ResetHits 전까지 한 번만 맞는다.
public class PiercingDamageDealer : DamageDealer
{
    private readonly HashSet<Transform> hitTargets = new();

    public void ResetHits() => hitTargets.Clear();

    protected override void OnHit(Transform target)
    {
        if (!IsTarget(target) || !hitTargets.Add(target))
            return;

        hit.Play((transform.position + target.position) / 2f);
        Attack(target);
    }

    // 적과 겹친 채로 방향을 바꿔도 돌아오는 길에 다시 맞을 수 있게 머무는 동안에도 검사한다.
    private void OnTriggerStay2D(Collider2D other) => OnHit(other.transform);

    private void OnDisable() => hitTargets.Clear();
}
