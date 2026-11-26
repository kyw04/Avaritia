// 무기를 든 공격자가 적중·휘두름 때 재생할 무기 피드백을 알려 준다.
// 사운드·파티클 매니저는 공격자가 누구인지 모르고 이 인터페이스만 보고 재생한다.
public interface IWeaponFeedbackSource
{
    // 무기로 맞혔을 때의 피드백. 무기가 없으면 false.
    bool TryGetWeaponHitFeedback(out ActionFeedback feedback);

    // 이 공격이 무기 콤보 공격일 때의 휘두름 피드백. 스킬 공격 등이면 false.
    bool TryGetWeaponSwingFeedback(AttackData attack, out ActionFeedback feedback);
}
