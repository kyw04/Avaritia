using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : Singleton<SoundManager>,
    IObserver<EntityJumpedEvent>,
    IObserver<EntityLandedEvent>,
    IObserver<EntityDashEvent>,
    IObserver<EntityDamagedEvent>,
    IObserver<EntityAttackStartEvent>
{
    private AudioSource sfxSource;

    protected override void Awake()
    {
        base.Awake();
        sfxSource = GetComponent<AudioSource>();
        EventBus.Subscribe<EntityJumpedEvent>(this);
        EventBus.Subscribe<EntityLandedEvent>(this);
        EventBus.Subscribe<EntityDashEvent>(this);
        EventBus.Subscribe<EntityDamagedEvent>(this);
        EventBus.Subscribe<EntityAttackStartEvent>(this);
    }

    public void OnNotify(EntityJumpedEvent e) => PlaySfx(e.Data.sound);
    public void OnNotify(EntityLandedEvent e) => PlaySfx(e.Source.Sounds.Land);
    public void OnNotify(EntityDashEvent e) => PlaySfx(e.Source.Sounds.Dash);
    public void OnNotify(EntityDamagedEvent e)
    {
        PlaySfx(e.Source.Sounds.Hit);
        if (e.Attacker is Player player && player.Weapon != null)
            PlaySfx(player.Weapon.hitSound);
    }

    // 스킬 공격(AttackEffect)도 같은 이벤트를 쓰므로 무기 콤보 공격일 때만 스윙 소리를 낸다.
    public void OnNotify(EntityAttackStartEvent e)
    {
        if (e.Source is Player player && player.Weapon != null && player.Weapon.combo != null
            && player.Weapon.combo.datas.Contains(e.Data))
            PlaySfx(player.Weapon.swingSound);
    }

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
            return;

        sfxSource.PlayOneShot(clip, volume);
    }
}
