using CometRun.Data;
using UnityEngine;
using UnityEngine.Audio;

namespace CometRun.Audio
{
    public enum VolumeChannel { Master, Music, SFX, UI }

    // Owns the Audio Mixer volumes. The settings sliders give me a value from 0 to 1,
    // but the mixer works in decibels, so I convert it with a log curve:
    //   dB = log10(value) * 20
    // 1 -> 0 dB (full), 0.5 -> about -6 dB, 0.1 -> -20 dB, and 0 -> -80 dB (silent).
    // A straight line (value * 80 - 80) sounded wrong, half the slider was basically silent.
    //
    // Runs before the other audio scripts so the saved volumes are set before any music starts.
    [DefaultExecutionOrder(-50)]
    public class MixerVolumes : MonoBehaviour
    {
        public static MixerVolumes Instance { get; private set; }

        public AudioMixer mixer;

        public const float MinDecibels = -80f;

        // names of the exposed parameters in GameMixer and the PlayerPrefs keys, same order as VolumeChannel
        static readonly string[] Parameters = { "MasterVolume", "MusicVolume", "SFXVolume", "UIVolume" };
        static readonly string[] SaveKeys = { "vol_master", "vol_music", "vol_sfx", "vol_ui" };
        static readonly float[] Defaults = { 0.8f, 0.7f, 0.8f, 0.8f };

        readonly float[] values = new float[4];

        void Awake()
        {
            Instance = this;
            for (int i = 0; i < values.Length; i++)
                values[i] = Mathf.Clamp01(PlayerPrefs.GetFloat(SaveKeys[i], Defaults[i]));
        }

        // SetFloat doesn't stick if you call it in Awake (known Unity thing), so it's done in Start
        void Start()
        {
            ApplyAll();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public float Get(VolumeChannel channel)
        {
            return values[(int)channel];
        }

        public void Set(VolumeChannel channel, float value)
        {
            int i = (int)channel;
            values[i] = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SaveKeys[i], values[i]);
            Apply(channel);
        }

        public void ApplyAll()
        {
            for (int i = 0; i < values.Length; i++) Apply((VolumeChannel)i);
        }

        void Apply(VolumeChannel channel)
        {
            if (mixer == null) return;
            float db = ToDecibels(values[(int)channel]);

            // the old Music / Sound Effects switches from Lab 7 now work as mute buttons
            if (channel == VolumeChannel.Music && !GameData.Music) db = MinDecibels;
            if (channel == VolumeChannel.SFX && !GameData.Sfx) db = MinDecibels;

            mixer.SetFloat(Parameters[(int)channel], db);
        }

        // log10(0) is minus infinity, so anything at (or very close to) zero just becomes -80 dB = muted
        public static float ToDecibels(float value)
        {
            if (value <= 0.0001f) return MinDecibels;
            return Mathf.Max(MinDecibels, Mathf.Log10(value) * 20f);
        }

        public static void SaveNow()
        {
            PlayerPrefs.Save();
        }
    }
}
