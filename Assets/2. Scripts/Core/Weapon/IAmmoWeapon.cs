// 탄약 바에 표시되는 총알 수를 노출한다. 어빌리티 조건/트리거가 무기 종류와 무관하게 쓴다.
public interface IAmmoWeapon
{
    int CurrentAmmo { get; }
    int MaxAmmo { get; }
    event System.Action<int, int> AmmoChanged; // (previous, current), 값이 실제로 바뀔 때만
}
