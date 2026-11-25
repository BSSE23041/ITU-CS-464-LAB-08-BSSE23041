using CometRun.Audio;
using UnityEngine;

namespace CometRun.Game
{
    // Listener #5: changes the music when the game state changes.
    // Run starts -> crossfade to the fast game music.
    // Run ends (won or lost) -> crossfade back to the calm menu music for the results screen.
    public class GameMusicSwitcher : MonoBehaviour
    {
        public GameSession session;

        void OnEnable()
        {
            session.GameStarted += OnStarted;
            session.GameWon += OnEnded;
            session.GameLost += OnEnded;
        }

        void OnDisable()
        {
            session.GameStarted -= OnStarted;
            session.GameWon -= OnEnded;
            session.GameLost -= OnEnded;
        }

        void OnStarted()
        {
            if (MusicPlayer.Instance != null) MusicPlayer.Instance.PlayGame();
        }

        void OnEnded(int score)
        {
            if (MusicPlayer.Instance != null) MusicPlayer.Instance.PlayMenu();
        }
    }
}
