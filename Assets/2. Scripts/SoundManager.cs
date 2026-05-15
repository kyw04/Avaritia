using UnityEngine;

public class SoundManager : MonoBehaviour,
    IObserver<PlayerJumpedEvent>,
    IObserver<PlayerLandedEvent>,
    IObserver<PlayerMovedEvent>
{
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;
    [SerializeField] private AudioClip moveClip;

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        EventBus.Subscribe<PlayerJumpedEvent>(this);
        EventBus.Subscribe<PlayerLandedEvent>(this);
        EventBus.Subscribe<PlayerMovedEvent>(this);
    }

    void OnDisable()
    {
        EventBus.UnsubscribeAll(this);
    }

    public void OnNotify(PlayerJumpedEvent e) { }
    public void OnNotify(PlayerLandedEvent e) { }
    public void OnNotify(PlayerMovedEvent e) { }
}