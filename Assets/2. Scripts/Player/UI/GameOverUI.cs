using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 런 결과 화면. 사망 연출이 끝나면 UIManager 스택에 올라오고(게임 정지), [다시 시작]으로 런을 초기화한다.
public class GameOverUI : Singleton<GameOverUI>, IObserver<RunResetEvent>
{
    public const string Key = "GameOverUI";

    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject firstButton;
    [SerializeField] private RunStatsTracker tracker;

    [Header("통계")]
    [SerializeField] private TMP_Text roomsText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text dealtText;
    [SerializeField] private TMP_Text takenText;

    [Header("사망 원인")]
    [SerializeField] private Image killerImage;
    [SerializeField] private TMP_Text killerNameText;

    [Header("최종 빌드")]
    [SerializeField] private Image weaponIcon;
    [SerializeField] private Image[] itemIcons;

    private UISelector selector;

    protected override void Awake()
    {
        base.Awake();
        UIManager.Instance.Register(Key,
            open: Open,
            close: Close);

        selector = new UISelector();
        EventBus.Subscribe<RunResetEvent>(this);
    }

    private void OnDestroy()
    {
        selector.Dispose();
        EventBus.UnsubscribeAll(this);
    }

    private void Open()
    {
        Fill();
        selector.SetActive(true);
        gameOverUI.SetActive(true);
        EventSystem.current.SetSelectedGameObject(firstButton);
    }

    private void Close()
    {
        selector.SetActive(false);
        gameOverUI.SetActive(false);
    }

    private void Fill()
    {
        roomsText.text = tracker.RoomsCleared.ToString();
        int seconds = Mathf.FloorToInt(tracker.PlayTime);
        timeText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        killsText.text = tracker.Kills.ToString();
        dealtText.text = Mathf.RoundToInt(tracker.DamageDealt).ToString("N0");
        takenText.text = Mathf.RoundToInt(tracker.DamageTaken).ToString("N0");

        killerNameText.text = tracker.KillerName;
        SetIcon(killerImage, tracker.KillerSprite);

        SetIcon(weaponIcon, tracker.WeaponIcon);
        for (int i = 0; i < itemIcons.Length; i++)
            SetIcon(itemIcons[i], i < tracker.ItemIcons.Count ? tracker.ItemIcons[i] : null);
    }

    private static void SetIcon(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    public void OnRestartButtonClick() => EventBus.Publish(new RunResetEvent());

    public void OnNotify(RunResetEvent e) => UIManager.Instance.PopUntil(Key);
}
