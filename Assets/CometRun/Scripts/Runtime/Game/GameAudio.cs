using UnityEngine;

namespace CometRun.Game
{
    // Listener #2: plays the gameplay sound effects when GameSession raises its events.
    //
    // Short sounds use PlayOneShot so they can overlap (tap 3 comets quickly = 3 sounds).
    // Pitch is set on the AudioSource though, so changing it would also change the sounds
    // that are still playing. That's why there are a few sources and I take turns using them.
    public class GameAudio : MonoBehaviour
    {
        public GameSession session;

        [Tooltip("All of these go to the SFX group of GameMixer")]
        public AudioSource[] sources;

        public AudioClip collect;
        public AudioClip boost;
        public AudioClip damage;
        public AudioClip death;
        public AudioClip tick;
        public AudioClip go;
        public AudioClip win;
        public AudioClip lose;

        int next;

        void OnEnable()
        {
            session.GameStarted += OnStarted;
            session.Collected += OnCollected;
            session.TimeBonus += OnTimeBonus;
            session.Damaged += OnDamaged;
            session.Died += OnDied;
            session.CountdownTick += OnTick;
            session.GameWon += OnWon;
            session.GameLost += OnLost;
        }

        void OnDisable()
        {
            session.GameStarted -= OnStarted;
            session.Collected -= OnCollected;
            session.TimeBonus -= OnTimeBonus;
            session.Damaged -= OnDamaged;
            session.Died -= OnDied;
            session.CountdownTick -= OnTick;
            session.GameWon -= OnWon;
            session.GameLost -= OnLost;
        }

        void OnStarted() => Play(go, 0f);
        void OnCollected(int score, Vector3 where) => Play(collect, 0.08f);
        void OnTimeBonus(float seconds, Vector3 where) => Play(boost, 0.03f);
        void OnDamaged(int livesLeft, Vector3 where) => Play(damage, 0.1f);
        void OnDied(Vector3 where) => Play(death, 0.04f);
        void OnTick(int secondsLeft) => Play(tick, 0f, 1f + (3 - secondsLeft) * 0.08f); // a bit higher each second
        void OnWon(int score) => Play(win, 0f);

        void OnLost(int score)
        {
            // if you died the explosion is already playing, the sad jingle on top was too much
            if (!session.DiedThisRun) Play(lose, 0f);
        }

        // pitchRange = how much random pitch to add, so the same sound isn't identical every time
        void Play(AudioClip clip, float pitchRange, float basePitch = 1f)
        {
            if (clip == null || sources.Length == 0) return;
            var source = sources[next];
            next = (next + 1) % sources.Length;
            source.pitch = basePitch + Random.Range(-pitchRange, pitchRange);
            source.PlayOneShot(clip);
        }
    }
}
