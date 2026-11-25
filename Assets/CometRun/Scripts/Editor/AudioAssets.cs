using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace CometRun.EditorTools
{
    // Helpers the scene builders use for sounds: loading the clips, finding the
    // mixer groups and adding AudioSources that are already routed to the right group.
    public static class AudioAssets
    {
        public const string Folder = "Assets/CometRun/Audio/";
        public const string MixerPath = Folder + "GameMixer.mixer";

        public static AudioMixer Mixer => AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);

        public static AudioClip Clip(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + name + ".wav");
            if (clip == null) Debug.LogWarning("Comet Run: missing sound " + name + ".wav (run Tools/make_audio.py)");
            return clip;
        }

        // "Master", "Music", "SFX" or "UI"
        public static AudioMixerGroup Group(string name)
        {
            var mixer = Mixer;
            if (mixer == null)
            {
                Debug.LogError("Comet Run: GameMixer.mixer not found at " + MixerPath);
                return null;
            }
            return mixer.FindMatchingGroups(name).FirstOrDefault(g => g.name == name);
        }

        public static AudioSource AddSource(GameObject go, string group, bool loop = false)
        {
            var source = go.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = Group(group);
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f; // 2D, it's a phone game
            return source;
        }

        // the music files are long so they stream instead of sitting decompressed in memory
        public static void SetupImportSettings()
        {
            foreach (var name in new[] { "music_menu", "music_game" })
            {
                var importer = AssetImporter.GetAtPath(Folder + name + ".wav") as AudioImporter;
                if (importer == null) continue;
                var settings = importer.defaultSampleSettings;
                if (settings.loadType == AudioClipLoadType.Streaming) continue;
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.6f;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }
    }
}
