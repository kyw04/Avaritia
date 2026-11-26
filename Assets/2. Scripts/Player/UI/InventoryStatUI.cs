using TMPro;
using UnityEngine;

// 인벤토리가 열려 있는 동안(이 오브젝트가 활성일 때) 플레이어의 최종 스탯(장비/버프 포함)을 표시한다.
// 버프는 바뀔 때 알리는 이벤트가 없어 매 프레임 값을 읽되, 값이 바뀐 텍스트만 다시 만든다.
public class InventoryStatUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI armorText;
    [SerializeField] private TextMeshProUGUI critRateText;
    [SerializeField] private TextMeshProUGUI evasionText;
    [SerializeField] private TextMeshProUGUI moveSpeedText;
    [SerializeField] private TextMeshProUGUI cooldownReductionText;

    private Player target;
    private float health = float.NaN, maxHealth = float.NaN, damage = float.NaN, critRate = float.NaN,
        evasion = float.NaN, moveSpeed = float.NaN, cooldownReduction = float.NaN;
    private int? armor;

    private void Awake()
    {
        target = FindAnyObjectByType<Player>();
    }

    private void Update()
    {
        if (target == null) return;

        float newHealth = target.CurrentHealth, newMaxHealth = target.MaxHealth;
        if (newHealth != health || newMaxHealth != maxHealth)
        {
            health = newHealth;
            maxHealth = newMaxHealth;
            healthText.text = $"{health:0}/{maxHealth:0}";
        }
        if (Changed(ref damage, target.Damage))
            damageText.text = $"{damage:0.#}";
        int newArmor = target.GetStat<int>(StatType.Armor);
        if (armor != newArmor)
        {
            armor = newArmor;
            armorText.text = newArmor.ToString();
        }
        if (Changed(ref critRate, target.GetStat<float>(StatType.CritRate)))
            critRateText.text = $"{critRate:0.#}%";
        if (Changed(ref evasion, target.GetStat<float>(StatType.Evasion)))
            evasionText.text = $"{evasion:0.#}%";
        if (Changed(ref moveSpeed, target.MoveSpeed))
            moveSpeedText.text = $"{moveSpeed:0.#}";
        if (Changed(ref cooldownReduction, target.GetStat<float>(StatType.CooldownReduction)))
            cooldownReductionText.text = $"{cooldownReduction:0.#}%";
    }

    private static bool Changed(ref float cached, float value)
    {
        if (cached == value) return false;
        cached = value;
        return true;
    }
}
