using UnityEngine;

public class SkillPickup : IInteractable
{
    private readonly SkillData skill;
    public SkillPickup(SkillData skill) => this.skill = skill;

    public string DisplayName => skill.name;
    public Sprite Icon => skill.icon;
    // Payload-only: never registered directly, always wrapped by WorldPickup.
    public Transform Transform => null;

    public bool NeedsChoice(Player player) =>
        player.Skills.SkillAt(0) != null && player.Skills.SkillAt(1) != null;

    public void Interact(Player player, InteractChoice choice)
    {
        int index = NeedsChoice(player)
            ? (choice == InteractChoice.Primary ? 0 : 1)
            : (player.Skills.SkillAt(0) == null ? 0 : 1);

        var previous = player.Skills.SetSkill(index, skill);
        if (previous != null)
            WorldInteractionManager.Instance.Spawn(new SkillPickup(previous), player.transform.position);
    }
}
