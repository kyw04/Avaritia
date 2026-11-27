using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

// Input System(키보드·패드)을 IPlayerInput으로 바꿔 전한다. 플레이어 명령 처리는 PlayerInputDriver가 한다.
// 인벤토리·일시정지·UI 입력은 UI 쪽이라 여기서 바로 처리한다.
// UISelector의 생성자가 다른 오브젝트의 Awake에서 InputHandler.Instance.InputAction을 바로 읽는다.
// InputHandler가 씬에 미리 배치된 컴포넌트라 Awake 순서가 보장되지 않으면 InputAction이 아직 null일 수 있어,
// 다른 스크립트보다 먼저 Awake되도록 강제한다.
[DefaultExecutionOrder(-1000)]
public class InputHandler : Singleton<InputHandler>, IPlayerInput
{
    // UI 입력(인벤토리·일시정지)을 막을 사망 여부 확인용.
    public Player player;
    private Vector2 moveInput;
    private readonly Dictionary<string, InputAction> gameplayActions = new();

    public PlayerInputActions InputAction { get; private set; }

    public event Action<PlayerInputSignal> ActionTriggered;

    public Vector2 MoveInput => moveInput;

    public bool IsPressed(string action) => gameplayActions.TryGetValue(action, out var a) && a.IsPressed();

    public bool IsControlOf(object control, string action) =>
        control is InputControl inputControl && gameplayActions.TryGetValue(action, out var a) && a.controls.ContainsReference(inputControl);

    // "Hold F | Hold Triangle" 대신 키보드 키 이름만 보여 준다.
    public string GetDisplayName(string action) =>
        gameplayActions.TryGetValue(action, out var a)
            ? a.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions, "Keyboard&Mouse")
            : string.Empty;

    protected override void OnAwake()
    {
        InputAction = new PlayerInputActions();
        foreach (var action in InputAction.Gameplay.Get().actions)
            gameplayActions[action.name] = action;
        _ = GameFreezeManager.Instance;
        _ = UIManager.Instance;
    }

    private void OnEnable()
    {
        InputAction.Enable();
        InputAction.UI.Disable();

        InputAction.Gameplay.Get().actionTriggered += Forward;
        InputAction.Gameplay.Inventory.started += OnInventoryOpen;
        InputAction.Gameplay.Pause.started += OnPauseOpen;

        InputAction.UI.Cancel.started += OnUICancel;
        InputAction.UI.CloseInventory.started += OnInventoryCloseAll;
    }

    private void OnDisable()
    {
        // 중복이라 지워지는 인스턴스는 OnAwake를 거치지 않아 InputAction이 없다.
        InputAction?.Disable();
    }

    private void Forward(InputAction.CallbackContext context)
    {
        // 같은 입력의 액션 콜백(인벤토리·일시정지 열기)이 먼저 Gameplay 맵을 끄면 context가 무효가 되므로 건너뛴다.
        if (!context.action.enabled) return;

        var action = context.action;
        var phase = (InputPhase)(int)context.phase;
        if (action.name == PlayerActions.Move)
            moveInput = phase == InputPhase.Canceled ? Vector2.zero : context.ReadValue<Vector2>();

        var control = context.control;
        ActionTriggered?.Invoke(new PlayerInputSignal(action.name, phase, control, control != null && control.IsPressed()));
    }

    // 사망 중에는 결과 화면을 Cancel로 닫거나 인벤토리·일시정지를 열 수 없다.
    private void OnUICancel(InputAction.CallbackContext context)
    {
        if (player.IsDead) return;
        UIManager.Instance.Pop();
    }
    private void OnInventoryOpen(InputAction.CallbackContext context)
    {
        if (player.IsDead) return;
        UIManager.Instance.Push(InventoryUI.Key);
    }
    private void OnInventoryCloseAll(InputAction.CallbackContext context)
    {
        if (ItemReplaceController.IsReplacing)
            ItemReplaceController.Instance.CancelReplace();
        UIManager.Instance.PopUntil(InventoryUI.Key);
    }
    private void OnPauseOpen(InputAction.CallbackContext context)
    {
        if (player.IsDead) return;
        UIManager.Instance.Push(PauseUI.Key);
    }
}
