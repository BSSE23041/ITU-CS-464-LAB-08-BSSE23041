using CometRun.Audio;
using CometRun.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CometRun.UI
{
    // Little message that pops up at the bottom (bought something, item locked etc).
    // Slides up, stays around 1.8 sec and fades away.
    public class Toast : MonoBehaviour
    {
        public CanvasGroup group;
        public Image background;
        public Text label;

        Vector2 home;
        bool homeSet;

        public void Show(string message, bool warning)
        {
            var rt = (RectTransform)transform;
            if (!homeSet)
            {
                home = rt.anchoredPosition;
                homeSet = true;
            }

            // warnings (locked, not enough coins...) get the error sound
            if (warning) UIAudio.Play(UISound.Error);

            label.text = message;
            background.color = warning ? UIPalette.DangerDark : UIPalette.SurfaceLight;
            transform.SetAsLastSibling();

            group.alpha = 0f;
            rt.anchoredPosition = home + new Vector2(0f, -40f);
            rt.MoveTo(home, 0.25f, Ease.OutCubic);
            group.FadeTo(1f, 0.2f, Ease.OutQuad);
            TweenRunner.Run(this, "hide", 0.25f, Ease.InQuad, k => group.alpha = 1f - k).SetDelay(1.8f);
        }
    }
}
