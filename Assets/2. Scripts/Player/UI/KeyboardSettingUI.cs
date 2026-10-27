using UnityEngine;
using UnityEngine.EventSystems;

public class KeyboardSettingUI : Singleton<KeyboardSettingUI>
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

}
