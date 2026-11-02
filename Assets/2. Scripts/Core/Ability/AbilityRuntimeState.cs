using System;

public class AbilityRuntimeState
{
    public float CooldownEndTime;
    public int ActivationCount;
    // 채널링 Effect가 진행 중이면 true. 그동안 같은 바인딩의 재발동은 무시되고, 바인딩이 해제되면 false로 돌려 채널링을 끊는다.
    public bool IsChanneling;
    // 키를 누르고 있어야 하는 채널링(ChannelInputMode.Hold)인지와 그 진행도(0~1). UI 표시용.
    public bool IsHoldChannel;
    public float ChannelProgress;
    // Effect가 Apply 중 true로 설정하면 TryActivate가 쿨다운을 시작하지 않는다. 대신 그 Effect가 나중에 StartCooldown을 호출한다.
    public bool DeferCooldown;
    public Action StartCooldown;
}
