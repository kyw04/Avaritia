using UnityEngine;
using UnityEngine.InputSystem;

public class TestInput : MonoBehaviour
{
    public PlayerInputActions input;
    private bool listening = false;
    
    private void Awake()
    {
        input = new PlayerInputActions();
    }
    
    private void OnEnable()
    {
        input.Enable();
    }

    void Update()
    {
        if (!listening) return;
        
        input.Gameplay.Jump.PerformInteractiveRebinding()
            .OnComplete(operation =>
            {
                Debug.Log(input.Gameplay.Jump.bindings[0].effectivePath);
                operation.Dispose();
            })
            .Start();
    }
    
    public void StartListen()
    {
        listening = true;
        input.Disable();
        Debug.Log("키 입력 대기 시작");
    }

    public void RebindJumpToJ()
    {
        var jumpAction = input.Gameplay.Jump;

        // 기존 바인딩 중 0번째를 J 키로 변경
        jumpAction.ApplyBindingOverride(0, "<Keyboard>/j");
        Debug.Log(input.Gameplay.Jump.bindings[0].effectivePath);
    }
}
