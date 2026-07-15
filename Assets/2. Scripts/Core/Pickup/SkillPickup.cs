using UnityEngine;

public class SkillPickup : IPickupable
{
    private readonly SkillData skill;
    public SkillPickup(SkillData skill) => this.skill = skill;

    public string DisplayName => skill.name;

    public bool NeedsChoice(Player player) =>
        player.Skills.SkillAt(0) != null && player.Skills.SkillAt(1) != null;

    public void Pickup(Player player, PickupChoice choice, Vector3 dropPosition)
    {
        int index = NeedsChoice(player)
            ? (choice == PickupChoice.Primary ? 0 : 1)
            : (player.Skills.SkillAt(0) == null ? 0 : 1);

        var previous = player.Skills.SetSkill(index, skill);
        if (previous != null)
            WorldPickupManager.Instance.Spawn(new SkillPickup(previous), dropPosition);
    }
}
