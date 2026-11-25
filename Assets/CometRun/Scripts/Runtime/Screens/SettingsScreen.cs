using CometRun.Audio;
using CometRun.Data;
using CometRun.Tweening;
using CometRun.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CometRun.Screens
{
    // 10 Settings. You can open it from the main menu or from pause,
    // and back takes you to whichever one you came from.
    //
    // Lab 8: the single volume slider became four (Master, Music, Effects, UI).
    // Each one controls a group in the Audio Mixer, see MixerVolumes for the dB maths.
    // The Music and Sound Effects switches are still here and work as mute buttons.
    public class SettingsScreen : UIScreen
    {
        public ToggleSwitch music;
        public ToggleSwitch sfx;
        public ToggleSwitch vibration;
        [Tooltip("Same order as VolumeChannel: Master, Music, SFX, UI")]
        public Slider[] volumeSliders;
        public Text[] volumeValues;
        public Button resetButton;
        public Text resetLabel;

        bool confirmReset;
        float lastTickTime;

        protected override void Awake()
        {
            base.Awake();
            music.Changed += v => { GameData.Music = v; GameData.Save(); ApplyMixer(); };
            sfx.Changed += v => { GameData.Sfx = v; GameData.Save(); ApplyMixer(); };
            vibration.Changed += v =>
            {
                GameData.Vibration = v;
                GameData.Save();
#if UNITY_ANDROID || UNITY_IOS
                if (v) Handheld.Vibrate();
#endif
            };

            for (int i = 0; i < volumeSliders.Length; i++)
            {
                var channel = (VolumeChannel)i;
                volumeSliders[i].onValueChanged.AddListener(v => OnVolumeChanged(channel, v));
            }
            resetButton.onClick.AddListener(OnReset);
        }

        public override void Refresh()
        {
            music.SetValue(GameData.Music, false);
            sfx.SetValue(GameData.Sfx, false);
            vibration.SetValue(GameData.Vibration, false);

            var volumes = MixerVolumes.Instance;
            for (int i = 0; i < volumeSliders.Length; i++)
            {
                float v = volumes != null ? volumes.Get((VolumeChannel)i) : 1f;
                volumeSliders[i].SetValueWithoutNotify(v);
                volumeValues[i].text = Percent(v);
            }
            CancelReset();
        }

        void OnVolumeChanged(VolumeChannel channel, float value)
        {
            if (MixerVolumes.Instance != null) MixerVolumes.Instance.Set(channel, value);
            volumeValues[(int)channel].text = Percent(value);

            // small tick while dragging so you can hear how loud it is now (goes through the UI group)
            if (Time.unscaledTime - lastTickTime > 0.09f)
            {
                lastTickTime = Time.unscaledTime;
                UIAudio.Play(UISound.Hover, 0.8f + value * 0.5f);
            }
        }

        static void ApplyMixer()
        {
            if (MixerVolumes.Instance != null) MixerVolumes.Instance.ApplyAll();
        }

        static string Percent(float v) => v <= 0.0001f ? "MUTE" : Mathf.RoundToInt(v * 100f) + "%";

        // the slider values are kept with PlayerPrefs.SetFloat while dragging,
        // and written to disk once when you leave the screen
        void OnDisable()
        {
            MixerVolumes.SaveNow();
        }

        // reset deletes everything so you have to tap twice (within 3 sec) to be safe
        void OnReset()
        {
            if (!confirmReset)
            {
                confirmReset = true;
                resetLabel.text = "TAP AGAIN TO CONFIRM";
                ((RectTransform)resetButton.transform).Shake(12f, 0.3f);
                Tweens.Wait(this, "resetTimeout", 3f).OnComplete(CancelReset);
                return;
            }

            GameData.ResetProgress();
            CancelReset();
            Nav.ShowToast("Progress reset", true);
        }

        void CancelReset()
        {
            confirmReset = false;
            resetLabel.text = "RESET PROGRESS";
        }
    }
}
