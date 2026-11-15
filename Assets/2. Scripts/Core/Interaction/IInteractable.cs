using UnityEngine;

public enum InteractChoice { Primary, Secondary }

// 이름·아이콘·설명은 IInfo로 제공한다(설명이 없으면 null).
public interface IInteractable : IInfo
{
    Transform Transform { get; }
    bool NeedsChoice(Player player);
    void Interact(Player player, InteractChoice choice);
}
