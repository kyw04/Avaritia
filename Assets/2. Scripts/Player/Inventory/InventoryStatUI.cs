using TMPro;
using UnityEngine;

// 인벤토리가 열려 있는 동안(이 오브젝트가 활성일 때) 매 프레임 플레이어의 최종 스탯(장비/버프 포함)을 표시한다.
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

    private void Awake()
    {
        target = FindAnyObjectByType<Player>();
    }

    private void Update()
    {
        if (target == null) return;

        healthText.text = $"{target.CurrentHealth:0}/{target.MaxHealth:0}";
        damageText.text = $"{target.Damage:0.#}";
        armorText.text = target.GetStat<int>(StatType.Armor).ToString();
        critRateText.text = $"{target.GetStat<float>(StatType.CritRate):0.#}%";
        evasionText.text = $"{target.GetStat<float>(StatType.Evasion):0.#}%";
        moveSpeedText.text = $"{target.MoveSpeed:0.#}";
        cooldownReductionText.text = $"{target.GetStat<float>(StatType.CooldownReduction):0.#}%";
    }
}
