using UnityEngine;

namespace CometRun.Audio
{
    public enum UISound { None, Hover, Click, Back, PanelOpen, PanelClose, Pause, Resume, Success, Error, Gem }

    // The one and only UI sound player. Buttons don't get their own AudioSource,
    // they just call UIAudio.Play(UISound.Click) and this plays it through the UI mixer group.
    // It also listens to GamePause so pausing / resuming always makes a sound,
    // no matter if it came from the button or the phone's back key.
    public class UIAudio : MonoBehaviour
    {
        public static UIAudio Instance { get; private set; }

        [Tooltip("Goes to the UI group of GameMixer")]
        public AudioSource source;

        public AudioClip hover;
        public AudioClip click;
        public AudioClip back;
        public AudioClip panelOpen;
        public AudioClip panelClose;
        public AudioClip pause;
        public AudioClip resume;
        public AudioClip success;
        public AudioClip error;
        public AudioClip gem;

        float lastHoverTime = -1f;

        void Awake()
        {
            Instance = this;
            // UI should still make sounds in the pause menu
            source.ignoreListenerPause = true;
            source.playOnAwake = false;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            GamePause.Changed += OnPauseChanged;
        }

        void OnDisable()
        {
            GamePause.Changed -= OnPauseChanged;
        }

        void OnPauseChanged(bool paused)
        {
            PlaySound(paused ? UISound.Pause : UISound.Resume, 1f);
        }

        // static so any script can call it without needing a reference
        public static void Play(UISound sound, float pitch = 1f)
        {
            if (Instance != null) Instance.PlaySound(sound, pitch);
        }

        void PlaySound(UISound sound, float pitch)
        {
            var clip = ClipFor(sound);
            if (clip == null) return;

            if (sound == UISound.Hover)
            {
                // moving the mouse over a list of buttons was way too clicky without this
                if (Time.unscaledTime - lastHoverTime < 0.06f) return;
                lastHoverTime = Time.unscaledTime;
            }

            // tiny random pitch change so pressing the same button 10 times doesn't sound robotic
            source.pitch = pitch * Random.Range(0.97f, 1.03f);
            source.PlayOneShot(clip);
        }

        AudioClip ClipFor(UISound sound)
        {
            switch (sound)
            {
                case UISound.Hover: return hover;
                case UISound.Click: return click;
                case UISound.Back: return back;
                case UISound.PanelOpen: return panelOpen;
                case UISound.PanelClose: return panelClose;
                case UISound.Pause: return pause;
                case UISound.Resume: return resume;
                case UISound.Success: return success;
                case UISound.Error: return error;
                case UISound.Gem: return gem;
                default: return null;
            }
        }
    }
}
