using System;
using System.Collections.Generic;

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

        if (wasEmpty)
        {
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

        if (stack.Count == 0)
        {
            InputHandler.Instance.InputAction.UI.Disable();
            InputHandler.Instance.InputAction.Gameplay.Enable();
        }
    }
}
