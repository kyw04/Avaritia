using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;

public class StartSceenManager : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName;
    
    private void OnEnable()
    {
        InputSystem.onAnyButtonPress.CallOnce(OnAnyButtonPress);
    }
    
    private void OnAnyButtonPress(InputControl control)
    {
        if (control is UnityEngine.InputSystem.Controls.ButtonControl)
        {
            Debug.Log($"입력 기기: {control.device.name} -> 씬 전환 시작");
            SceneManager.LoadScene(gameplaySceneName);
        }
        else
        {
            InputSystem.onAnyButtonPress.CallOnce(OnAnyButtonPress);
        }
    }
}
