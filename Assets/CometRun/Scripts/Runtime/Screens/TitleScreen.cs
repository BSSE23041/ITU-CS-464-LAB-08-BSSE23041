using CometRun.Audio;
using CometRun.Tweening;
using CometRun.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CometRun.Screens
{
    // 01 Title screen, tap anywhere to go to the main menu
    public class TitleScreen : UIScreen
    {
        public Button tapArea;
        public RectTransform logo;
        public CanvasGroup tapHint;
        public UIScreen mainMenu;

        protected override void Awake()
        {
            base.Awake();
            tapArea.onClick.AddListener(() =>
            {
                if (Nav.Busy) return;
                UIAudio.Play(UISound.Click);
                Nav.ResetTo(mainMenu);
            });
        }

        public override void OnEnter()
        {
            // logo pops in with a little overshoot since it's the first thing you should look at
            logo.localScale = Vector3.one * 0.7f;
            logo.ScaleTo(Vector3.one, 0.6f, Ease.OutBack);

            // "tap anywhere" shows up after the logo and keeps slowly pulsing
            // so you know it's waiting for you
            tapHint.alpha = 0f;
            tapHint.FadeTo(1f, 0.4f, Ease.OutQuad).SetDelay(0.6f).OnComplete(() =>
                TweenRunner.Run(tapHint, "fade", 1.1f, Ease.InOutSine, k => tapHint.alpha = Mathf.Lerp(1f, 0.35f, k)).SetPingPong());
        }

        public override void OnBack()
        {
            Application.Quit();
        }
    }
}
