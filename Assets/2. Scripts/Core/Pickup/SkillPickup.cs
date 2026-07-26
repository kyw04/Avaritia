using UnityEngine;

public class SkillPickup : IInteractable
{
    private readonly SkillData skill;
    public SkillPickup(SkillData skill) => this.skill = skill;

    public string DisplayName => skill.name;
    public Sprite Icon => skill.icon;

    public bool NeedsChoice(Player player) =>
        player.Skills.SkillAt(0) != null && player.Skills.SkillAt(1) != null;

    public void Interact(Player player, InteractChoice choice, Vector3 dropPosition)
    {
        int index = NeedsChoice(player)
            ? (choice == InteractChoice.Primary ? 0 : 1)
            : (player.Skills.SkillAt(0) == null ? 0 : 1);

        var previous = player.Skills.SetSkill(index, skill);
        if (previous != null)
            WorldPickupManager.Instance.Spawn(new SkillPickup(previous), dropPosition);
    }
}
