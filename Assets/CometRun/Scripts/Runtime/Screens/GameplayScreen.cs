using CometRun.Audio;
using CometRun.Data;
using CometRun.Game;
using CometRun.Tweening;
using CometRun.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CometRun.Screens
{
    // 06 Gameplay HUD. In Lab 7 this was only a mock-up, now the real game runs
    // behind it (Gameplay scene, loaded by the loading screen).
    //
    // This is listener #1 of the GameSession events: it updates the score, lives,
    // red damage flash and the banner text. When the run ends it opens Results.
    public class GameplayScreen : UIScreen
    {
        public Button pauseButton;
        public Text levelText;
        public Text scoreText;
        public Text timeText;
        public RectTransform timeFill;
        public Image timeFillImage;
        public Image[] lifeIcons;
        public Image damageFlash;
        public CanvasGroup banner;
        public Text bannerText;

        public UIScreen pause;
        public ResultsScreen results;

        GameSession session;
        bool restartPending;
        bool endHandled;

        public string Summary => levelText.text + "  |  Score " + (session != null ? session.Score : 0);

        protected override void Awake()
        {
            base.Awake();
            pauseButton.onClick.AddListener(OpenPause);
        }

        void OnEnable()
        {
            session = GameSession.Current;
            if (session == null) return;
            session.GameStarted += OnGameStarted;
            session.Collected += OnCollected;
            session.Damaged += OnDamaged;
            session.GameWon += OnGameWon;
            session.GameLost += OnGameLost;
        }

        void OnDisable()
        {
            if (session == null) return;
            session.GameStarted -= OnGameStarted;
            session.Collected -= OnCollected;
            session.Damaged -= OnDamaged;
            session.GameWon -= OnGameWon;
            session.GameLost -= OnGameLost;
        }

        public override void Refresh()
        {
            levelText.text = GameData.Mode == GameMode.Endless ? "ENDLESS" : "LEVEL " + (GameData.SelectedLevel + 1);
            if (session != null)
            {
                scoreText.text = session.Score.ToString();
                UpdateLives(session.Lives);
            }
        }

        public override void OnEnter()
        {
            endHandled = false;
            restartPending = false;
            damageFlash.color = UIPalette.WithAlpha(UIPalette.Danger, 0f);
            if (session != null) session.BeginRun();
        }

        // pause screen closed (resume or restart)
        public override void OnReturn()
        {
            if (Nav.Top != this) return; // came back from settings, pause is still open on top
            GamePause.Set(false);
            if (restartPending)
            {
                restartPending = false;
                session.Restart();
                Nav.ShowToast("Run restarted");
            }
        }

        public override void OnBack()
        {
            OpenPause();
        }

        void OpenPause()
        {
            if (session == null || !session.IsRunning || Nav.Busy) return;
            GamePause.Set(true);
            Nav.Push(pause);
        }

        void Update()
        {
            if (session == null) return;
            float t = session.TimeLeft;
            timeText.text = Mathf.CeilToInt(t) + "s";
            timeFill.anchorMax = new Vector2(Mathf.Clamp01(t / Mathf.Max(1f, session.TotalTime)), 1f);
            bool hurry = session.IsRunning && t <= 3f;
            timeFillImage.color = hurry ? UIPalette.Danger : UIPalette.Primary;
            timeText.color = hurry ? UIPalette.Danger : UIPalette.Text;
        }

        // ---- called from the pause menu ----

        public void RestartRun()
        {
            restartPending = true; // done in OnReturn, after the pause panel has closed
            Nav.Back();
        }

        public void QuitRun(UIScreen mainMenu)
        {
            if (session != null) session.Abort();
            GamePause.ResetState(); // not Set(false), the resume sound would play while leaving
            if (MusicPlayer.Instance != null) MusicPlayer.Instance.PlayMenu();
            if (Nav.BackTo(mainMenu)) UnloadAfterSlide();
        }

        // ---- event handlers ----

        void OnGameStarted()
        {
            scoreText.text = "0";
            UpdateLives(session.Lives);
            ShowBanner("GO!", UIPalette.Primary, 0.7f);
        }

        void OnCollected(int score, Vector3 where)
        {
            scoreText.text = score.ToString();
            scoreText.transform.Punch(0.15f, 0.2f);
        }

        void OnDamaged(int livesLeft, Vector3 where)
        {
            UpdateLives(livesLeft);
            if (livesLeft >= 0 && livesLeft < lifeIcons.Length) lifeIcons[livesLeft].transform.Punch(0.4f, 0.3f);

            // quick red flash over the whole screen
            damageFlash.color = UIPalette.WithAlpha(UIPalette.Danger, 0.35f);
            damageFlash.ColorTo(UIPalette.WithAlpha(UIPalette.Danger, 0f), 0.35f, Ease.OutQuad);
        }

        void OnGameWon(int score) => EndRun(GameData.Mode == GameMode.Endless ? "RUN OVER" : "TIME UP!", UIPalette.Primary);

        void OnGameLost(int score) => EndRun(session.DiedThisRun ? "OUT OF LIVES" : "TIME UP!", UIPalette.Danger);

        // show the banner for a moment so you can see what happened, then go to results
        void EndRun(string message, Color color)
        {
            if (endHandled) return;
            endHandled = true;
            ShowBanner(message, color, 1.3f);
            Tweens.Wait(this, "toResults", 1.4f).OnComplete(GoToResults);
        }

        void GoToResults()
        {
            var result = GameData.RecordResult(session.Score, session.DiedThisRun);
            results.SetResult(result);
            Nav.Replace(results);
            UnloadAfterSlide();
        }

        // wait for the slide animation, otherwise the game would vanish while still on screen
        void UnloadAfterSlide()
        {
            Tweens.Wait(ScreenManager.Instance, "unloadGame", ScreenManager.SlideTime + 0.05f)
                .OnComplete(() => GameSceneLoader.Unload());
        }

        void ShowBanner(string message, Color color, float hold)
        {
            bannerText.text = message;
            bannerText.color = color;
            banner.alpha = 0f;
            banner.transform.localScale = Vector3.one * 0.7f;
            banner.transform.ScaleTo(Vector3.one, 0.3f, Ease.OutBack);
            banner.FadeTo(1f, 0.15f, Ease.OutQuad).OnComplete(() =>
                banner.FadeTo(0f, 0.3f, Ease.InQuad).SetDelay(hold));
        }

        void UpdateLives(int lives)
        {
            for (int i = 0; i < lifeIcons.Length; i++)
                lifeIcons[i].color = i < lives ? UIPalette.Danger : UIPalette.Disabled;
        }
    }
}
