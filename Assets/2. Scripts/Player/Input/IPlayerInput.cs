using System;
using UnityEngine;

// 입력 단계. 값은 InputActionPhase와 같게 둬서 무기 에셋에 직렬화된 값(2, 3, 4)을 그대로 읽는다.
public enum InputPhase
{
    Started = 2,
    Performed = 3,
    Canceled = 4,
}

// 플레이어가 쓰는 Gameplay 액션 이름. 입력 신호와 무기 바인딩이 이 이름으로 맞춰진다.
public static class PlayerActions
{
    public const string Move = "Move";
    public const string Jump = "Jump";
    public const string Attack = "Attack";
    public const string DownAttack = "DownAttack";
    public const string DownHold = "DownHold";
    public const string Dash = "Dash";
    public const string Skill1 = "Skill1";
    public const string Skill2 = "Skill2";
    public const string Interact = "Interact";
}

// 액션 하나의 단계 변화.
public readonly struct PlayerInputSignal
{
    public readonly string Action;
    public readonly InputPhase Phase;
    // 이 신호를 낸 물리 입력(키 등). 입력을 모르는 쪽은 IPlayerInput.IsControlOf에 넘기기만 한다. 없으면 null.
    public readonly object Control;
    // Control이 지금 눌려 있는지.
    public readonly bool ControlPressed;

    public PlayerInputSignal(string action, InputPhase phase, object control = null, bool controlPressed = false)
    {
        Action = action;
        Phase = phase;
        Control = control;
        ControlPressed = controlPressed;
    }
}

// 플레이어를 조종하는 입력. 키보드·패드(InputHandler) 외에 AI, 리플레이, 테스트가 같은 방식으로 플레이어를 움직일 수 있다.
public interface IPlayerInput
{
    // 행동 제한을 적용하기 전의 이동 입력.
    Vector2 MoveInput { get; }

    bool IsPressed(string action);

    // 액션이 시작·실행·취소될 때마다 알린다.
    event Action<PlayerInputSignal> ActionTriggered;

    // control이 action에 묶인 입력인지. 아래 방향키처럼 여러 액션이 같은 키를 쓰는지 판정한다.
    bool IsControlOf(object control, string action);

    // 키 안내 UI에 보일 이름. 보여 줄 키가 없는 입력(AI 등)은 빈 문자열.
    string GetDisplayName(string action);
}
