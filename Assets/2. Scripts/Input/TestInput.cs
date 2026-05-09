using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestInput : MonoBehaviour
{
    public TextMeshProUGUI text;
    private InputAction action;
    
    void Update()
    {
        text.text = action?.GetBindingDisplayString();
    }
    
    public void StartListen(InputActionReference actionReference)
    {       
        action = InputHandler.Instance.inputAction.FindAction(actionReference.action.name);
        int bindingIndex = FindKeyboardBindingIndex(action, "Keyboard");
        if (bindingIndex == -1)
            return;
        
        InputActionMap map = action.actionMap;
        map.Disable();
        
        Debug.Log(action.name);
        
        action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsHavingToMatchPath("<Keyboard>")
            .WithCancelingThrough("<Keyboard>/escape")
            .WithControlsExcluding("Mouse")
            .OnComplete(operation =>
            {
                operation.Dispose();
                action.Enable();
            })
            .Start();
    }
    
    int FindKeyboardBindingIndex(InputAction targetAction, string targetController)
    {
        for (int i = 0; i < targetAction.bindings.Count; i++)
        {
            InputBinding binding = targetAction.bindings[i];

            if (binding.effectivePath.Contains(targetController))
            {
                return i;
            }
        }

        return -1;
    }
}
