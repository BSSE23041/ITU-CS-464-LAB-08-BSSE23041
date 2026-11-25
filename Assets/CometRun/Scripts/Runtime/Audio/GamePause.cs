using System;
using UnityEngine;

namespace CometRun.Audio
{
    // One place that pauses and resumes the game.
    //
    // What happens to the audio while paused (I picked this on purpose):
    //   - Gameplay sounds (SFX group) stop, because AudioListener.pause pauses every
    //     AudioSource that doesn't have ignoreListenerPause ticked.
    //   - Music keeps playing but gets quieter (MusicPlayer listens to Changed).
    //   - UI sounds keep working so the pause menu buttons still click.
    //     Both the music and UI sources have ignoreListenerPause = true.
    public static class GamePause
    {
        public static bool IsPaused { get; private set; }

        public static event Action<bool> Changed;

        public static void Set(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            Changed?.Invoke(paused);
        }

        // used when the game starts so nothing is left paused from before
        public static void ResetState()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
