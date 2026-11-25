using System.IO;
using CometRun.Game;
using CometRun.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CometRun.EditorTools
{
    // Builds Gameplay.unity, the scene the loading screen loads.
    // Same idea as the menu scene builder: if something breaks, run
    // Comet Run > Build Gameplay Scene and it gets made again.
    //
    // Everything is made from Unity's basic shapes:
    //   comet = sphere with two smaller spheres as a trail
    //   gold comet = smaller yellow sphere
    //   asteroid = red cube that spins
    //   stars = tiny quads in the back
    // There is no camera here, the menu scene's camera draws it (the scene is loaded additively).
    public static class GameSceneBuilder
    {
        public const string ScenePath = "Assets/CometRun/Scenes/Gameplay.unity";
        const string MaterialFolder = "Assets/CometRun/Materials/";

        [MenuItem("Comet Run/Build Gameplay Scene")]
        public static void Build()
        {
            Directory.CreateDirectory(MaterialFolder);
            AudioAssets.SetupImportSettings();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cometMat = Mat("Comet", UIPalette.Primary);
            var trailMat = Mat("CometTrail", UIPalette.Hex(0xB45309));
            var goldMat = Mat("Gold", UIPalette.Gold);
            var asteroidMat = Mat("Asteroid", UIPalette.Danger);
            var starMat = Mat("Star", UIPalette.Hex(0x475569));

            // ---- background stars ----
            var stars = new GameObject("Stars");
            var rnd = new System.Random(11);
            for (int i = 0; i < 45; i++)
            {
                var star = Shape(PrimitiveType.Quad, "Star", stars.transform, starMat, false);
                float size = 0.04f + (float)rnd.NextDouble() * 0.06f;
                star.transform.localScale = new Vector3(size, size, 1f);
                star.transform.position = new Vector3((float)rnd.NextDouble() * 6.4f - 3.2f, (float)rnd.NextDouble() * 11f - 5.5f, 5f);
            }

            // ---- game session + the templates it copies ----
            var sessionGo = new GameObject("GameSession");
            var session = sessionGo.AddComponent<GameSession>();
            var templates = new GameObject("Templates").transform;
            templates.SetParent(sessionGo.transform);
            var spawned = new GameObject("Spawned").transform;
            spawned.SetParent(sessionGo.transform);
            session.spawnRoot = spawned;

            // comet: orange ball with a trail going up (it falls down)
            var comet = Shape(PrimitiveType.Sphere, "Comet", templates, cometMat, true);
            comet.transform.localScale = Vector3.one * 0.8f;
            ((SphereCollider)comet.GetComponent<Collider>()).radius = 0.8f; // bigger than the ball so it's easy to tap
            Trail(comet.transform, trailMat, 0.62f, 0.6f);
            Trail(comet.transform, trailMat, 1.02f, 0.35f);
            session.cometTemplate = Target(comet, TargetKind.Comet);

            // gold comet: smaller and faster, gives extra time
            var gold = Shape(PrimitiveType.Sphere, "GoldComet", templates, goldMat, true);
            gold.transform.localScale = Vector3.one * 0.55f;
            ((SphereCollider)gold.GetComponent<Collider>()).radius = 1f;
            Trail(gold.transform, goldMat, 0.7f, 0.5f);
            session.goldTemplate = Target(gold, TargetKind.Gold);

            // asteroid: red spinning cube, don't tap it
            var asteroid = Shape(PrimitiveType.Cube, "Asteroid", templates, asteroidMat, true);
            asteroid.transform.localScale = Vector3.one * 0.62f;
            asteroid.transform.rotation = Quaternion.Euler(25f, 35f, 0f);
            ((BoxCollider)asteroid.GetComponent<Collider>()).size = Vector3.one * 1.3f;
            session.asteroidTemplate = Target(asteroid, TargetKind.Asteroid);

            // ---- listeners ----
            var audioGo = new GameObject("GameAudio");
            var gameAudio = audioGo.AddComponent<GameAudio>();
            gameAudio.session = session;
            gameAudio.sources = new[]
            {
                AudioAssets.AddSource(audioGo, "SFX"),
                AudioAssets.AddSource(audioGo, "SFX"),
                AudioAssets.AddSource(audioGo, "SFX"),
                AudioAssets.AddSource(audioGo, "SFX"),
            };
            gameAudio.collect = AudioAssets.Clip("sfx_collect");
            gameAudio.boost = AudioAssets.Clip("sfx_boost");
            gameAudio.damage = AudioAssets.Clip("sfx_damage");
            gameAudio.death = AudioAssets.Clip("sfx_death");
            gameAudio.tick = AudioAssets.Clip("sfx_tick");
            gameAudio.go = AudioAssets.Clip("sfx_go");
            gameAudio.win = AudioAssets.Clip("sfx_win");
            gameAudio.lose = AudioAssets.Clip("sfx_lose");

            var particlesGo = new GameObject("HitParticles");
            var hitParticles = particlesGo.AddComponent<HitParticles>();
            hitParticles.session = session;
            hitParticles.particles = MakeParticles(particlesGo);

            var shakeGo = new GameObject("CameraShake");
            shakeGo.AddComponent<CameraShake>().session = session;

            var musicGo = new GameObject("GameMusicSwitcher");
            musicGo.AddComponent<GameMusicSwitcher>().session = session;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Comet Run: gameplay scene built at " + ScenePath);
        }

        static Material Mat(string name, Color color)
        {
            string path = MaterialFolder + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                // unlit so it looks flat like the UI and doesn't need any lights
                mat = new Material(Shader.Find("Unlit/Color"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = color;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static GameObject Shape(PrimitiveType type, string name, Transform parent, Material mat, bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static void Trail(Transform parent, Material mat, float up, float scale)
        {
            var t = Shape(PrimitiveType.Sphere, "Trail", parent, mat, false);
            t.transform.localPosition = new Vector3(0f, up, 0.3f);
            t.transform.localScale = Vector3.one * scale;
        }

        static CometTarget Target(GameObject go, TargetKind kind)
        {
            var target = go.AddComponent<CometTarget>();
            target.kind = kind;
            return target;
        }

        static ParticleSystem MakeParticles(GameObject go)
        {
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;

            var emission = ps.emission;
            emission.rateOverTime = 0f; // only bursts from code

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
            return ps;
        }
    }
}
