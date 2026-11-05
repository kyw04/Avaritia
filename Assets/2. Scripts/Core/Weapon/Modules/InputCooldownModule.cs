using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 입력 쿨타임 모듈: action/phase가 같은 입력 바인딩의 메서드를 쿨타임 동안 실행하지 않는다.
// startOnInput을 끄면 입력만으로는 쿨이 돌지 않고, 다른 모듈이 WeaponInstance.StartInputCooldown으로 시작한다
// (원치처럼 적에게 걸렸을 때만, 자석 흡인처럼 끝날 때 쿨을 주는 경우). 쿨타임 중에는 icon을 상태 아이콘 UI에 띄운다.
[Serializable]
public class InputCooldownModule : WeaponModule
{
    [SerializeField] private InputActionReference action;
    [SerializeField] private InputActionPhase phase = InputActionPhase.Performed;
    [SerializeField, Min(0)] private float cooldown = 1f;
    [SerializeField] private Sprite icon;
    [SerializeField] private bool startOnInput = true;

    // 무기 인스턴스에 남으므로 버렸다 다시 주워도 쿨이 이어진다.
    private float readyTime;

    private bool IsCoolingDown => Time.time < readyTime;

    public void StartCooldown() => readyTime = Time.time + cooldown;

    public bool Matches(WeaponData.InputMethodBinding binding) =>
        action != null && binding.action != null && binding.action.action.id == action.action.id && binding.phase == phase;

    // 떼는 입력(Canceled)은 막지 않는다. 누른 상태를 끝내는 입력이 막히면 상태가 풀리지 않는다.
    public override bool CanExecute(WeaponData.InputMethodBinding binding) =>
        binding.phase == InputActionPhase.Canceled || !Matches(binding) || !IsCoolingDown;

    public override void OnExecuted(WeaponData.InputMethodBinding binding)
    {
        if (startOnInput && Matches(binding))
            StartCooldown();
    }

    public override void CollectStatusIcons(List<StatusIconEntry> into)
    {
        if (icon == null || !IsCoolingDown)
            return;

        into.Add(new StatusIconEntry
        {
            Key = (this, StatusIconKind.Cooldown),
            Icon = icon,
            EndTime = readyTime,
            Duration = cooldown,
        });
    }
}
