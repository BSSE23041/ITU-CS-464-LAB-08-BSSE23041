using UnityEngine;

namespace CometRun.Audio
{
    // Background music with crossfading. It has two AudioSources: when the track changes
    // the new one fades in on the free source while the old one fades out, so there's
    // never a hard cut. Menu music plays in the menus, game music during a run, and it
    // goes back to the menu music when the run ends (results screen).
    //
    // While the game is paused the music keeps going but drops to pausedVolume.
    public class MusicPlayer : MonoBehaviour
    {
        public static MusicPlayer Instance { get; private set; }

        public AudioClip menuMusic;
        public AudioClip gameMusic;

        [Tooltip("Both go to the Music group of GameMixer")]
        public AudioSource sourceA;
        public AudioSource sourceB;

        public float fadeTime = 1.2f;
        [Range(0f, 1f)] public float pausedVolume = 0.35f;

        AudioSource active;
        float levelA, levelB;    // current fade level of each source (0..1)
        float targetA, targetB;  // where each one is fading to
        float duck = 1f;

        void Awake()
        {
            Instance = this;
            foreach (var s in new[] { sourceA, sourceB })
            {
                s.loop = true;
                s.playOnAwake = false;
                s.volume = 0f;
                s.ignoreListenerPause = true; // music is allowed to keep playing in the pause menu
            }
        }

        void Start()
        {
            PlayMenu();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void PlayMenu() => Play(menuMusic);
        public void PlayGame() => Play(gameMusic);

        public void Play(AudioClip clip)
        {
            if (clip == null) return;
            if (active != null && active.clip == clip && TargetOf(active) > 0f) return; // already on this song

            var next = active == sourceA ? sourceB : sourceA;
            if (next.clip != clip || !next.isPlaying)
            {
                next.clip = clip;
                next.Play();
                SetLevel(next, 0f);
            }

            SetTarget(next, 1f);
            if (active != null) SetTarget(active, 0f);
            active = next;
        }

        void Update()
        {
            // unscaled time because timeScale is 0 while paused and the fade should still finish
            float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime);
            levelA = Mathf.MoveTowards(levelA, targetA, step);
            levelB = Mathf.MoveTowards(levelB, targetB, step);

            float duckTarget = GamePause.IsPaused ? pausedVolume : 1f;
            duck = Mathf.MoveTowards(duck, duckTarget, Time.unscaledDeltaTime * 3f);

            sourceA.volume = levelA * duck;
            sourceB.volume = levelB * duck;

            // stop the faded out one so it isn't playing silently forever
            if (levelA <= 0f && targetA <= 0f && sourceA.isPlaying) sourceA.Stop();
            if (levelB <= 0f && targetB <= 0f && sourceB.isPlaying) sourceB.Stop();
        }

        float TargetOf(AudioSource s) => s == sourceA ? targetA : targetB;

        void SetTarget(AudioSource s, float value)
        {
            if (s == sourceA) targetA = value; else targetB = value;
        }

        void SetLevel(AudioSource s, float value)
        {
            if (s == sourceA) levelA = value; else levelB = value;
        }
    }
}
