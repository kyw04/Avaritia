using UnityEngine;
using UnityEngine.EventSystems;

public class PauseUI : Singleton<PauseUI>
{
    public const string Key = "PauseUI";
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject firstButton;
    
    private UISelector selector;

    protected override void Awake()
    {
        base.Awake();
        UIManager.Instance.Register(Key,
            open: Open,
            close: Close);

        selector = new UISelector();
    }
    private void OnDestroy() => selector.Dispose();
    
    private void Open()
    {
        selector.SetActive(true);
        pauseUI.SetActive(true);
        EventSystem.current.SetSelectedGameObject(firstButton);
    }

    private void Close()
    {
        selector.SetActive(false);
        pauseUI.SetActive(false);
    }

    public void OnCancelButtonClick()
    {
        UIManager.Instance.Pop();
    }

    public void OnSettingButtonClick()
    {
        Debug.Log("OnSettingButtonClick");
    }
    
    public void OnGameRestartButtonClick()
    {
        Debug.Log("OnGameRestartButtonClick");
    }

    public void OnQuitButtonClick()
    {
        if (ConfirmationPopup.IsBlockingInput) return;
        Debug.Log($"OnQuitButtonClick");
        ConfirmationPopup.Instance.Show("게임을 종료하시겠습니까?", Application.Quit);
    }
}