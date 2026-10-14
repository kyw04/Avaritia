using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : Singleton<SoundManager>,
    IObserver<EntityJumpedEvent>,
    IObserver<EntityLandedEvent>,
    IObserver<EntityDashEvent>,
    IObserver<EntityDamagedEvent>
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

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
            return;

        sfxSource.PlayOneShot(clip, volume);
    }
}
