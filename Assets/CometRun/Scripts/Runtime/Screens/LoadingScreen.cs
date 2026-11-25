using System.Collections;
using CometRun.Data;
using CometRun.Game;
using CometRun.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CometRun.Screens
{
    // 11 Loading. Sits between the hangar (START RUN) and the game.
    // It loads Gameplay.unity in the background with LoadSceneAsync and shows the real progress.
    //
    // allowSceneActivation = false keeps the scene from switching on by itself. Unity stops
    // the progress at 0.9 while it waits, so I divide by 0.9 to get a proper 0-100%.
    // Only when the bar is full do I let it activate, and only after that does the gameplay screen open.
    public class LoadingScreen : UIScreen
    {
        public RectTransform barFill;
        public Text percentText;
        public Text statusText;
        public Text infoText;
        public Text tipText;
        public RectTransform spinner;

        public GameplayScreen gameplay;

        [Tooltip("How fast the bar is allowed to move (per second). It never goes past the real progress.")]
        public float barSpeed = 1.6f;

        static readonly string[] Tips =
        {
            "Tap the orange comets for points",
            "Gold comets give you +2 seconds",
            "Red cubes are asteroids. Don't tap them!",
            "You have 3 lives. Each asteroid takes one",
            "Better ships give more points per comet",
        };

        bool loading;

        public override void Refresh()
        {
            SetBar(0f);
            statusText.text = "LOADING";
            infoText.text = GameData.Mode == GameMode.Endless
                ? "Endless run"
                : GameData.ModeName(GameData.Mode) + "  |  Level " + (GameData.SelectedLevel + 1);
            tipText.text = "TIP: " + Tips[Random.Range(0, Tips.Length)];
        }

        public override void OnEnter()
        {
            if (!loading) StartCoroutine(LoadGame());
        }

        // you can't back out of a loading screen, it only takes a moment anyway
        public override void OnBack() { }

        IEnumerator LoadGame()
        {
            loading = true;

            // wait for the slide-in animation, the screen manager ignores clicks while it's busy
            while (Nav.Busy) yield return null;

            // just in case an old copy of the game is still loaded
            var unload = GameSceneLoader.Unload();
            if (unload != null) yield return unload;

            var op = GameSceneLoader.LoadAsync();
            op.allowSceneActivation = false;

            float shown = 0f;
            while (true)
            {
                float real = Mathf.Clamp01(op.progress / 0.9f);
                // smooth it a little so it doesn't jump straight from 0 to 100,
                // but it's never ahead of what Unity actually loaded
                shown = Mathf.MoveTowards(shown, real, barSpeed * Time.unscaledDeltaTime);
                SetBar(shown);
                if (real >= 1f && shown >= 1f) break;
                yield return null;
            }

            statusText.text = "STARTING";
            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;

            statusText.text = "READY";
            yield return new WaitForSecondsRealtime(0.2f);

            while (Nav.Busy) yield return null;
            loading = false;
            Nav.Replace(gameplay);
        }

        void Update()
        {
            // little comet going round so you can see the game didn't freeze
            if (spinner != null) spinner.Rotate(0f, 0f, -270f * Time.unscaledDeltaTime);
        }

        void SetBar(float value)
        {
            barFill.anchorMax = new Vector2(value, 1f);
            percentText.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        void OnDisable()
        {
            // if the screen gets turned off for some reason, don't leave the flag stuck
            loading = false;
        }
    }
}
