using UnityEngine;

// 한 번의 Weapon.Attack 동안 모듈들이 공유하는 공격 정보.
// Enhancement는 공격 시작 이벤트 중 EnhanceCurrentAttack으로 채워진다.
public class WeaponAttack
{
    public IAttacker Attacker;
    public int Index;
    public Transform Defender;
    public AttackData Data;
    public BulletEnhancement Enhancement;
}
