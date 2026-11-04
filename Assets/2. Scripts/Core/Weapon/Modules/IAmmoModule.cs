using System;

// 탄약을 가진 모듈이 구현한다. 탄약 UI나 어빌리티 조건이 모듈 종류와 무관하게 탄약을 읽는 데 쓴다.
public interface IAmmoModule
{
    int CurrentAmmo { get; }
    int MaxAmmo { get; }
    event Action<int, int> AmmoChanged;
    bool GainPaused { get; set; }
    void Consume();
}
