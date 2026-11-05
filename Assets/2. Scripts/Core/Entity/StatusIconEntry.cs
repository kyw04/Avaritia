using UnityEngine;

public enum StatusIconKind { Buff, Shield, HitBlock, Invincible, Cooldown, Debuff }

// 상태 아이콘 UI(StatusIconUI)에 띄울 한 칸. Entity/AbilityManager가 매 프레임 채워 준다.
public struct StatusIconEntry
{
    // 같은 아이콘을 프레임 사이에 같은 칸으로 이어 붙이기 위한 식별자.
    public (object source, StatusIconKind kind) Key;
    public Sprite Icon;
    // float.MaxValue = 만료 없음(영구/조건부/잠긴 쿨타임).
    public float EndTime;
    // 0보다 크고 EndTime이 유한할 때만 채움 오버레이를 표시한다.
    public float Duration;
    // 0보다 크면 숫자로 표시(스택 수, 실드 잔량, 남은 무효화 횟수).
    public int Count;

    public StatusIconKind Kind => Key.kind;
    public bool IsTimed => Duration > 0f && EndTime < float.MaxValue;
}
