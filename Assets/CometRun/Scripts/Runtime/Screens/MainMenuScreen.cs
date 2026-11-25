using CometRun.Audio;
using CometRun.Data;
using CometRun.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CometRun.Screens
{
    // 02 Main menu, everything else is opened from here
    public class MainMenuScreen : UIScreen
    {
        public Button playButton;
        public Button shopButton;
        public Button settingsButton;
        public Button quitButton;
        public Text progressText;
        public RectTransform[] entrance;

        public UIScreen modeSelect;
        public UIScreen shop;
        public UIScreen settings;
        public UIScreen title;

        protected override void Awake()
        {
            base.Awake();
            playButton.onClick.AddListener(() => Nav.Push(modeSelect));
            shopButton.onClick.AddListener(() => Nav.Push(shop));
            settingsButton.onClick.AddListener(() => Nav.Push(settings));
            quitButton.onClick.AddListener(Quit);
        }

        public override void Refresh()
        {
            progressText.text = "Levels cleared: " + GameData.LevelsCleared() + " / " + GameData.LevelCount;
        }

        public override void OnEnter()
        {
            PlayStagger(entrance);
        }

        public override void OnBack()
        {
            UIAudio.Play(UISound.Back);
            Nav.ResetTo(title);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
