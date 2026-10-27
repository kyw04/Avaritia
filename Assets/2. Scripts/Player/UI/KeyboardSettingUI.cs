using UnityEngine;
using UnityEngine.EventSystems;

public class KeyboardSettingUI : Singleton<KeyboardSettingUI>
{
    public const string Key = "KeyboardSettingUI";
    [SerializeField] private GameObject keyboardSetting;
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
        keyboardSetting.SetActive(true);
        EventSystem.current.SetSelectedGameObject(firstButton);
    }

    private void Close()
    {
        selector.SetActive(false);
        keyboardSetting.SetActive(false);
    }

}
