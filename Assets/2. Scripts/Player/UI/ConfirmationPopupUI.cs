using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ConfirmationPopup : Singleton<ConfirmationPopup>
{
    public const string Key = "Confirm";
    public static bool IsConfirming { get; private set; }
    public static bool IsBlockingInput => IsConfirming || closedFrame == Time.frameCount;
    private static int closedFrame = -1;
    private int openedFrame = -1;

    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private GameObject cancelButton;

    private UISelector selector;
    private Action onConfirm;
    private Action onCancel;
    private GameObject previousSelected;

    protected override void Awake()
    {
        base.Awake();
        selector = new UISelector
        {
            Submit = OnSubmit
        };

        UIManager.Instance.Register(Key, open: Open, close: Close);
    }

    private void OnDestroy() => selector.Dispose();

    public void Show(string message, Action onConfirm, Action onCancel = null)
    {
        if (IsConfirming) return;

        this.onConfirm = onConfirm;
        this.onCancel = onCancel;
        messageText.text = message;
        previousSelected = EventSystem.current.currentSelectedGameObject;
        UIManager.Instance.Push(Key);
    }

    public void OnSubmit()
    {
        if (!IsConfirming || openedFrame == Time.frameCount) return;

        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == cancelButton)
        {
            var cancel = onCancel;
            UIManager.Instance.Pop();
            cancel?.Invoke();
        }
        else
        {
            var confirm = onConfirm;
            UIManager.Instance.Pop();
            confirm?.Invoke();
        }
    }

    private void Open()
    {
        IsConfirming = true;
        openedFrame = Time.frameCount;
        panel.SetActive(true);
        selector.SetActive(true);
        EventSystem.current.SetSelectedGameObject(cancelButton);
    }

    private void Close()
    {
        if (!IsConfirming) return;

        IsConfirming = false;
        closedFrame = Time.frameCount;
        panel.SetActive(false);
        selector.SetActive(false);
        onConfirm = null;
        onCancel = null;

        EventSystem.current.SetSelectedGameObject(previousSelected);
    }
}
