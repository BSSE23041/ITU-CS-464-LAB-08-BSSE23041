using System;
using CometRun.Audio;
using CometRun.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CometRun.UI
{
    // On/off switch for settings. I show the state with the knob side, the colour
    // AND the ON/OFF text, because in greyscale the colour alone wasn't enough.
    public class ToggleSwitch : MonoBehaviour
    {
        public Button button;
        public Image track;
        public RectTransform knob;
        public Text stateText;
        public bool isOn = true;

        public event Action<bool> Changed;

        const float KnobX = 38f;

        void Awake()
        {
            button.onClick.AddListener(() =>
            {
                SetValue(!isOn, true);
                UIAudio.Play(UISound.Click, isOn ? 1.1f : 0.9f); // higher for on, lower for off
                Changed?.Invoke(isOn);
            });
        }

        public void SetValue(bool on, bool animate)
        {
            isOn = on;
            var knobPos = new Vector2(on ? KnobX : -KnobX, 0f);
            Color trackColor = on ? UIPalette.Primary : UIPalette.Disabled;
            stateText.text = on ? "ON" : "OFF";
            stateText.color = on ? UIPalette.Text : UIPalette.TextMuted;

            if (animate)
            {
                knob.MoveTo(knobPos, 0.18f, Ease.InOutQuad);
                track.ColorTo(trackColor, 0.18f, Ease.InOutQuad);
            }
            else
            {
                knob.anchoredPosition = knobPos;
                track.color = trackColor;
            }
        }
    }
}
