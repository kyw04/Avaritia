using UnityEngine;

// 탄창 무기의 특정 총알 한 발에 적용되는 강화 값. EnhanceBulletEffect가 MagazineWeapon에 넘겨준다.
[System.Serializable]
public class BulletEnhancement
{
    public float damageMultiplier = 2f;
    public float speedMultiplier = 1f;
    public float scaleMultiplier = 1f;
    public BulletMover bulletPrefab; // 대신 발사할 총알. 비우면 공격의 기본 총알
    public GaugeSlotLook ammoLook = new(); // 탄약 게이지에서 이 총알 칸의 색·무늬. use를 끄면 기본 칸
}
