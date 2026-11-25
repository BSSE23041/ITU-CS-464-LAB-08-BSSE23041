using UnityEngine;
using UnityEngine.SceneManagement;

namespace CometRun.Game
{
    // The game itself is in its own scene (Gameplay.unity). It gets loaded on top of the
    // menu scene (additive) so the menus, pause, settings and results from Lab 7 keep working
    // exactly like before, and gets unloaded again when the run is over.
    public static class GameSceneLoader
    {
        public const string SceneName = "Gameplay";

        public static bool IsLoaded => SceneManager.GetSceneByName(SceneName).isLoaded;

        public static AsyncOperation LoadAsync()
        {
            return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
        }

        public static AsyncOperation Unload()
        {
            if (!IsLoaded) return null;
            return SceneManager.UnloadSceneAsync(SceneName);
        }
    }
}
