using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum AudioType
{
    None,
    Music,
    Effect,
}

public class SoundManager : Singleton<SoundManager>,
    IObserver<EntityJumpedEvent>,
    IObserver<EntityLandedEvent>,
    IObserver<EntityTurnEvent>,
    IObserver<EntityTurnEndedEvent>,
    IObserver<EntityDashEvent>,
    IObserver<EntityDamagedEvent>,
    IObserver<EntityAttackStartEvent>,
    IObserver<PlayerEquippedEvent>,
    IObserver<InventoryItemUnequip>,
    IObserver<UIPanelOpenedEvent>,
    IObserver<UIPanelClosedEvent>
{
    [SerializeField, Range(0, 1)] private float volume = 1f;
    [SerializeField, Range(0, 1)] private float musicVolume = 1f;
    [SerializeField, Range(0, 1)] private float effectVolume = 1f;
    
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource effectSource;

    [SerializeField] private AudioClip equipSound; // 무기/스킬/아이템 장착 시
    [SerializeField] private AudioClip dropSound; // 인벤토리에서 아이템/스킬을 버릴 때
    [SerializeField] private AudioClip uiOpenSound;
    [SerializeField] private AudioClip uiCloseSound;
    [SerializeField] private AudioClip uiHoverSound; // UI 선택 이동 시
    [SerializeField] private AudioClip uiSelectSound; // UI Submit 입력 시
    [SerializeField] private AudioClip uiCancelSound; // UI Cancel 입력 시

    private readonly Dictionary<AudioType, AudioSource> sources = new();
    // 턴(미끄러짐) 소리는 턴이 이어지는 동안 반복 재생하고, 끝나면 짧게 줄여 멈춘다.
    private AudioSource loopSource;
    private Entity loopOwner;
    private float loopGain;
    private float loopTarget;
    private PlayerInputActions inputAction;
    private GameObject lastSelected;
    // 패널을 열고 닫을 때 코드가 선택을 옮기므로, 그 직후 잠깐은 hover 소리를 내지 않는다.
    private int panelChangedFrame = -10;
    private int panelSoundFrame = -10;
    private AudioClip pendingInputSound;

    protected override void Awake()
    {
        base.Awake();
        
        EventBus.Subscribe<EntityJumpedEvent>(this);
        EventBus.Subscribe<EntityLandedEvent>(this);
        EventBus.Subscribe<EntityTurnEvent>(this);
        EventBus.Subscribe<EntityTurnEndedEvent>(this);
        EventBus.Subscribe<EntityDashEvent>(this);
        EventBus.Subscribe<EntityDamagedEvent>(this);
        EventBus.Subscribe<EntityAttackStartEvent>(this);
        EventBus.Subscribe<PlayerEquippedEvent>(this);
        EventBus.Subscribe<InventoryItemUnequip>(this);
        EventBus.Subscribe<UIPanelOpenedEvent>(this);
        EventBus.Subscribe<UIPanelClosedEvent>(this);

        inputAction = InputHandler.Instance.InputAction;
        inputAction.UI.Submit.performed += OnUISubmit;
        inputAction.UI.Cancel.started += OnUICancel;

        sources[AudioType.Music] = musicSource;
        sources[AudioType.Effect] = effectSource;

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
        loopSource.loop = true;
        if (effectSource != null)
        {
            loopSource.outputAudioMixerGroup = effectSource.outputAudioMixerGroup;
            loopSource.spatialBlend = effectSource.spatialBlend;
        }
    }

    private void OnDestroy()
    {
        if (inputAction == null) return;
        inputAction.UI.Submit.performed -= OnUISubmit;
        inputAction.UI.Cancel.started -= OnUICancel;
    }

    // 같은 입력으로 패널이 열리거나 닫히면 그 소리가 대신 나야 하므로, 프레임 끝(LateUpdate)까지 미뤘다가 판단한다.
    private void OnUISubmit(UnityEngine.InputSystem.InputAction.CallbackContext context) => pendingInputSound = uiSelectSound;
    private void OnUICancel(UnityEngine.InputSystem.InputAction.CallbackContext context) => pendingInputSound = uiCancelSound;

    private void LateUpdate()
    {
        if (pendingInputSound == null) return;
        if (panelSoundFrame != Time.frameCount)
            PlayOneShotClip(pendingInputSound, AudioType.Effect);
        pendingInputSound = null;
    }

    public void VolumeSetting(float mul, float music, float sfx)
    {
        volume = mul;
        musicVolume = music;
        effectVolume = sfx;
        
        musicSource.volume = musicVolume * volume;
        effectSource.volume = effectVolume * volume;
    }

    public void OnNotify(EntityJumpedEvent e) => PlayOneShotClip(e.Data.jump.sound, AudioType.Effect);
    public void OnNotify(EntityLandedEvent e) => PlayOneShotClip(e.Source.Feedbacks.Land.sound, AudioType.Effect);
    public void OnNotify(EntityTurnEvent e)
    {
        var clip = e.Source.Feedbacks.Turn.sound;
        if (clip == null || loopSource == null) return;

        loopOwner = e.Source;
        loopTarget = 1f;
        if (loopSource.clip != clip || !loopSource.isPlaying)
        {
            loopSource.clip = clip;
            loopSource.time = 0f;
            loopGain = 0f;
            loopSource.Play();
        }
    }

    public void OnNotify(EntityTurnEndedEvent e)
    {
        if (e.Source == loopOwner) loopTarget = 0f;
    }

    private void UpdateTurnLoop()
    {
        if (loopSource == null || !loopSource.isPlaying && loopTarget == 0f) return;

        // 턴 도중 주인이 사라지거나 꺼지면(사망 등) Exit 이벤트가 안 올 수 있다.
        if (loopOwner == null || !loopOwner.isActiveAndEnabled) loopTarget = 0f;

        // 게임이 멈춘 동안(일시정지/인벤토리)은 소리도 멈춘다.
        if (Time.timeScale == 0f) { loopSource.Pause(); return; }
        loopSource.UnPause();

        float fadeTime = loopTarget > loopGain ? 0.03f : 0.1f;
        loopGain = Mathf.MoveTowards(loopGain, loopTarget, Time.unscaledDeltaTime / fadeTime);
        loopSource.volume = (effectSource != null ? effectSource.volume : 1f) * volume * loopGain;
        if (loopGain <= 0f && loopTarget == 0f) loopSource.Stop();
    }
    public void OnNotify(EntityDashEvent e) => PlayOneShotClip(e.Source.Feedbacks.Dash.sound, AudioType.Effect);
    public void OnNotify(PlayerEquippedEvent e) => PlayOneShotClip(equipSound, AudioType.Effect);
    public void OnNotify(InventoryItemUnequip e) => PlayOneShotClip(dropSound, AudioType.Effect);
    // 확인 팝업은 선택/취소 소리만 내고 열기/닫기 소리는 내지 않는다.
    // 다른 패널은 열기/닫기 소리가 같은 프레임의 선택/취소 소리를 대신한다(LateUpdate 참고).
    public void OnNotify(UIPanelOpenedEvent e) => PlayPanelSound(uiOpenSound, e.Key);
    public void OnNotify(UIPanelClosedEvent e) => PlayPanelSound(uiCloseSound, e.Key);

    // PopUntil로 여러 패널이 한 번에 닫혀도 한 번만 울린다.
    private void PlayPanelSound(AudioClip clip, string key)
    {
        panelChangedFrame = Time.frameCount;
        if (key == ConfirmationPopup.Key || panelSoundFrame == Time.frameCount) return;
        panelSoundFrame = Time.frameCount;
        PlayOneShotClip(clip, AudioType.Effect);
    }

    private void Update()
    {
        UpdateTurnLoop();

        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected == lastSelected) return;

        bool moved = lastSelected != null && selected != null;
        lastSelected = selected;
        if (moved && Time.frameCount - panelChangedFrame > 1)
            PlayOneShotClip(uiHoverSound, AudioType.Effect);
    }
    public void OnNotify(EntityDamagedEvent e)
    {
        PlayOneShotClip(e.Source.Feedbacks.Hit.sound, AudioType.Effect);
        if (e.Attacker is Player player && player.Weapon != null)
            PlayOneShotClip(player.Weapon.Data.hit.sound, AudioType.Effect);
    }

    // 스킬 공격(AttackEffect)도 같은 이벤트를 쓰므로 무기 콤보 공격일 때만 스윙 소리를 낸다.
    public void OnNotify(EntityAttackStartEvent e)
    {
        if (e.Source is Player player && player.Weapon != null && player.Weapon.Data.combo != null
            && player.Weapon.Data.combo.datas.Contains(e.Data))
            PlayOneShotClip(player.Weapon.Data.swing.sound, AudioType.Effect);
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
