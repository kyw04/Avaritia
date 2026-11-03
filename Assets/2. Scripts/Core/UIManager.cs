using System;
using System.Collections.Generic;

public struct UIPanelOpenedEvent : ISubject
{
    public string Key { get; private set; }
    public UIPanelOpenedEvent(string key) { Key = key; }
}

public struct UIPanelClosedEvent : ISubject
{
    public string Key { get; private set; }
    public UIPanelClosedEvent(string key) { Key = key; }
}

public class UIManager : Singleton<UIManager>
{
    private class Panel
    {
        public Action Open;
        public Action Close;
    }

    private readonly Dictionary<string, Panel> panels = new();
    private readonly List<string> stack = new();

    public void Register(string key, Action open, Action close)
        => panels[key] = new Panel { Open = open, Close = close };

    public void Push(string key)
    {
        if (!panels.TryGetValue(key, out var panel)) return;

        bool wasEmpty = stack.Count == 0;
        stack.Add(key);
        panel.Open();
        EventBus.Publish(new UIPanelOpenedEvent(key));

        if (wasEmpty)
        {
            GameFreezeManager.Instance.Freeze();
            InputHandler.Instance.InputAction.Gameplay.Disable();
            InputHandler.Instance.InputAction.UI.Enable();
        }
    }

    public void Pop()
    {
        if (stack.Count == 0) return;

        var key = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        panels[key].Close();
        EventBus.Publish(new UIPanelClosedEvent(key));

        if (stack.Count == 0)
        {
            GameFreezeManager.Instance.Unfreeze();
            InputHandler.Instance.InputAction.UI.Disable();
            InputHandler.Instance.InputAction.Gameplay.Enable();
        }
    }

    // key 패널 위에 쌓인 패널들을 모두 닫은 뒤 key 패널까지 닫는다. 스택에 없으면 아무것도 하지 않는다.
    public void PopUntil(string key)
    {
        int index = stack.LastIndexOf(key);
        if (index < 0) return;

        while (stack.Count > index)
            Pop();
    }
}
