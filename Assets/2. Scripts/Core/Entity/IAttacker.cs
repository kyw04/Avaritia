using UnityEngine;

public interface IAttacker
{
    MonoBehaviour Mono { get; }
    float Damage { get; }
    bool IsAttacking { get; set; }
    int LookDirection { get; }
    // 공격이 캔슬될 때마다 바뀐다. 진행 중인 공격은 시작할 때 값과 달라지면 멈춘다.
    int AttackCancelVersion { get; }
    // 공격 준비·지속 시간에 곱하는 속도 배율(공격 둔화 등).
    float AttackSpeedMultiplier { get; }
    // 공격 판정 레이어를 공격자 편(아군/적)에 맞게 바꾼다.
    LayerMask ResolveTargetLayer(LayerMask mask);
    // 대상에 따라 달라지는 버프까지 반영한 피해.
    float GetDamageAgainst(Transform target);
}
