using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class SkillImage
{
    public Image image;
    public Image fill;

    [NonSerialized] public float EndTime;
    [NonSerialized] public float Duration;
}

public class SkillUI : MonoBehaviour, IObserver<EntitySkillCooldownEvent>, IObserver<EntitySkillEquippedEvent>
{
    [SerializeField] private SkillImage Skill1;
    [SerializeField] private SkillImage Skill2;

    private Player target;

    private void Awake()
    {
        target = FindAnyObjectByType<Player>();
        EventBus.Subscribe<EntitySkillCooldownEvent>(this);
        EventBus.Subscribe<EntitySkillEquippedEvent>(this);
    }

    private void Start()
    {
        if (target == null) return;
        SetIcon(Skill1, target.Skills.SkillAt(0));
        SetIcon(Skill2, target.Skills.SkillAt(1));
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<EntitySkillCooldownEvent>(this);
        EventBus.Unsubscribe<EntitySkillEquippedEvent>(this);
    }

    public void OnNotify(EntitySkillCooldownEvent e)
    {
        if (e.Source != target) return;

        var slot = e.SlotIndex == 0 ? Skill1 : e.SlotIndex == 1 ? Skill2 : null;
        if (slot == null) return;
        slot.EndTime = e.EndTime;
        slot.Duration = e.Duration;
    }

    public void OnNotify(EntitySkillEquippedEvent e)
    {
        if (e.Source != target) return;

        var slot = e.SlotIndex == 0 ? Skill1 : e.SlotIndex == 1 ? Skill2 : null;
        if (slot == null) return;
        SetIcon(slot, e.Skill);
    }

    private static void SetIcon(SkillImage slot, SkillData skill)
    {
        if (slot?.image == null) return;
        slot.image.sprite = skill != null ? skill.icon : null;
        slot.image.enabled = skill != null;
    }

    private void Update()
    {
        UpdateFill(Skill1);
        UpdateFill(Skill2);
    }

    private static void UpdateFill(SkillImage slot)
    {
        if (slot?.fill == null || slot.Duration <= 0f) return;
        slot.fill.fillAmount = Mathf.Clamp01((slot.EndTime - Time.time) / slot.Duration);
    }
}
