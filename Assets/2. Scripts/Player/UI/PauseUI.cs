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
        if (ConfirmationPopup.IsBlockingInput) return;
        ConfirmationPopup.Instance.Show("준비중입니다.", () => { });
    }
    
    public void OnGameRestartButtonClick()
    {
        if (ConfirmationPopup.IsBlockingInput) return;
        ConfirmationPopup.Instance.Show("처음부터 다시 시작하시겠습니까?", () =>
        {
            UIManager.Instance.PopUntil(Key);
            EventBus.Publish(new RunResetEvent());
        });
    }

    public void OnQuitButtonClick()
    {
        if (ConfirmationPopup.IsBlockingInput) return;
        Debug.Log($"OnQuitButtonClick");
        ConfirmationPopup.Instance.Show("게임을 종료하시겠습니까?", Application.Quit);
    }
}