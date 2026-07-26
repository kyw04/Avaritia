using UnityEngine;

public enum InteractChoice { Primary, Secondary }

public interface IInteractable
{
    string DisplayName { get; }
    Sprite Icon { get; }
    bool NeedsChoice(Player player);
    void Interact(Player player, InteractChoice choice, Vector3 dropPosition);
}
