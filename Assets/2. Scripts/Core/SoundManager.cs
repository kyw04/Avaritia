using System.Collections.Generic;
using UnityEngine;

public enum AudioType
{
    None,
    Music,
    Effect,
}

public class SoundManager : Singleton<SoundManager>,
    IObserver<EntityJumpedEvent>,
    IObserver<EntityLandedEvent>,
    IObserver<EntityDashEvent>,
    IObserver<EntityDamagedEvent>,
    IObserver<EntityAttackStartEvent>
{
    [SerializeField, Range(0, 1)] private float volume = 1f;
    [SerializeField, Range(0, 1)] private float musicVolume = 1f;
    [SerializeField, Range(0, 1)] private float effectVolume = 1f;
    
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource effectSource;

    private readonly Dictionary<AudioType, AudioSource> sources = new();
    
    protected override void Awake()
    {
        base.Awake();
        
        EventBus.Subscribe<EntityJumpedEvent>(this);
        EventBus.Subscribe<EntityLandedEvent>(this);
        EventBus.Subscribe<EntityDashEvent>(this);
        EventBus.Subscribe<EntityDamagedEvent>(this);
        EventBus.Subscribe<EntityAttackStartEvent>(this);
        
        sources[AudioType.Music] = musicSource;
        sources[AudioType.Effect] = effectSource;
    }

    public void VolumeSetting(float mul, float music, float sfx)
    {
        volume = mul;
        musicVolume = music;
        effectVolume = sfx;
        
        musicSource.volume = musicVolume * volume;
        effectSource.volume = effectVolume * volume;
    }

    public void OnNotify(EntityJumpedEvent e) => PlayOneShotClip(e.Data.sound, AudioType.Effect);
    public void OnNotify(EntityLandedEvent e) => PlayOneShotClip(e.Source.Sounds.Land, AudioType.Effect);
    public void OnNotify(EntityDashEvent e) => PlayOneShotClip(e.Source.Sounds.Dash, AudioType.Effect);
    public void OnNotify(EntityDamagedEvent e)
    {
        PlayOneShotClip(e.Source.Sounds.Hit, AudioType.Effect);
        if (e.Attacker is Player player && player.Weapon != null)
            PlayOneShotClip(player.Weapon.hitSound, AudioType.Effect);
    }

    // 스킬 공격(AttackEffect)도 같은 이벤트를 쓰므로 무기 콤보 공격일 때만 스윙 소리를 낸다.
    public void OnNotify(EntityAttackStartEvent e)
    {
        if (e.Source is Player player && player.Weapon != null && player.Weapon.combo != null
            && player.Weapon.combo.datas.Contains(e.Data))
            PlayOneShotClip(player.Weapon.swingSound, AudioType.Effect);
    }

    public void PlayOneShotClip(AudioClip clip, AudioType type, float mul = 1f)
    {
        if (clip == null || sources[type] == null)
            return;

        sources[type].PlayOneShot(clip, volume * mul);
    }

    public void PlayClip(AudioClip clip, AudioType type)
    {
        if (clip == null || sources[type] == null)
            return;
        
        sources[type].clip = clip;
        sources[type].Play();
    }
}
