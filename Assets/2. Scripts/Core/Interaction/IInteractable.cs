using UnityEngine;

public enum InteractChoice { Primary, Secondary }

public interface IInteractable
{
    string DisplayName { get; }
    Sprite Icon { get; }
    Transform Transform { get; }
    bool NeedsChoice(Player player);
    void Interact(Player player, InteractChoice choice);
}
