using CoreSystem;
using UnityEngine;
using UnityEngine.UI;

namespace UiSystem
{
    public sealed class GameUi : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private GameObject menuPanel, hudPanel, gameOverPanel, touchControls;
        [SerializeField] private Text timeText, killsText, healthText, finalTimeText, finalKillsText;
        [SerializeField] private Image healthFill;
        [SerializeField] private Button playButton, restartButton;
        private int displayedSecond = -1;
        public Button PlayButton => playButton;
        public Button RestartButton => restartButton;
        public GameObject MenuPanel => menuPanel;
        public GameObject GameOverPanel => gameOverPanel;
        public void Configure(GameSession game, GameObject menu, GameObject hud, GameObject end, GameObject touch,
            Text time, Text kills, Text hp, Text finalTime, Text finalKills, Image fill, Button play, Button restart)
        {
            session = game; menuPanel = menu; hudPanel = hud; gameOverPanel = end; touchControls = touch;
            timeText = time; killsText = kills; healthText = hp; finalTimeText = finalTime; finalKillsText = finalKills;
            healthFill = fill; playButton = play; restartButton = restart;
        }
        private void OnEnable()
        {
            session.StateChanged += OnStateChanged;
            session.StatsChanged += RefreshStats;
            session.Player.HealthChanged += RefreshHealth;
            playButton.onClick.AddListener(session.StartSession);
            restartButton.onClick.AddListener(session.StartSession);
        }
        private void OnDisable()
        {
            session.StateChanged -= OnStateChanged;
            session.StatsChanged -= RefreshStats;
            session.Player.HealthChanged -= RefreshHealth;
            playButton.onClick.RemoveListener(session.StartSession);
            restartButton.onClick.RemoveListener(session.StartSession);
        }
        private void Start() { OnStateChanged(session.State); RefreshHealth(session.Player.Health, session.Player.MaxHealth); }
        private void Update()
        {
            if (session.State != SessionState.Playing) return;
            int second = Mathf.FloorToInt(session.SurvivalTime);
            if (displayedSecond == second) return;
            displayedSecond = second;
            timeText.text = FormatTime(second);
        }
        private void OnStateChanged(SessionState state)
        {
            menuPanel.SetActive(state == SessionState.Menu);
            hudPanel.SetActive(state == SessionState.Playing);
            touchControls.SetActive(state == SessionState.Playing);
            gameOverPanel.SetActive(state == SessionState.GameOver);
            displayedSecond = -1;
            if (state == SessionState.GameOver)
            {
                finalTimeText.text = FormatTime(Mathf.FloorToInt(session.SurvivalTime));
                finalKillsText.text = session.Kills.ToString("00");
                restartButton.Select();
            }
            else if (state == SessionState.Menu) playButton.Select();
            else UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
            RefreshStats();
        }
        private void RefreshStats() => killsText.text = session.Kills.ToString("00");
        private void RefreshHealth(float current, float maximum)
        {
            healthText.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(maximum);
            healthFill.fillAmount = maximum > 0 ? current / maximum : 0;
            healthFill.color = current / maximum > .3f ? new Color(.35f, .94f, .77f) : new Color(1, .3f, .31f);
        }
        public static string FormatTime(int seconds) => (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
    }
}
