using System;
using CometRun.Audio;
using CometRun.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CometRun.Game
{
    // The actual game (lives in the Gameplay scene).
    // Comets (orange balls) fall from the top, tap them for points.
    // Gold ones also give extra time. Red cubes are asteroids, tapping one costs a life.
    // Run ends when the timer hits 0 or you lose all 3 lives.
    //
    // This class does NOT play sounds, spawn particles, shake the camera or touch the HUD.
    // It only raises C# events and other components listen to them:
    //   GameplayScreen (HUD), GameAudio (sounds), HitParticles, CameraShake, GameMusicSwitcher.
    public class GameSession : MonoBehaviour
    {
        // the gameplay scene is loaded additively, so the menu scene finds the game through this
        public static GameSession Current { get; private set; }

        [Header("Things to spawn (kept turned off in the scene)")]
        public CometTarget cometTemplate;
        public CometTarget asteroidTemplate;
        public CometTarget goldTemplate;
        public Transform spawnRoot;

        [Header("Rules")]
        public int startLives = 3;
        public float goldBonusSeconds = 2f;
        [Range(0f, 1f)] public float asteroidChance = 0.25f;
        [Range(0f, 1f)] public float goldChance = 0.06f;

        // ---- events ----
        public event Action GameStarted;
        public event Action<int, Vector3> Collected;     // new score, where it happened
        public event Action<float, Vector3> TimeBonus;    // seconds added, where
        public event Action<int, Vector3> Damaged;       // lives left, where
        public event Action<Vector3> Died;
        public event Action<int> CountdownTick;          // 3, 2, 1 at the end of the run
        public event Action<int> GameWon;                // final score
        public event Action<int> GameLost;               // final score

        public int Score { get; private set; }
        public int Lives { get; private set; }
        public float TimeLeft { get; private set; }
        public float TotalTime { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsOver { get; private set; }
        public bool DiedThisRun { get; private set; }

        Camera cam;
        float spawnTimer;
        float spawnEvery;
        float fallSpeed;
        int pointsPerComet;
        int lastTick;
        bool boostUsed;

        void Awake()
        {
            Current = this;
            cometTemplate.gameObject.SetActive(false);
            asteroidTemplate.gameObject.SetActive(false);
            goldTemplate.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        // called by the HUD when the gameplay screen opens
        public void BeginRun()
        {
            // the Extra Time boost from the shop is used up once per run (not again on restart)
            float extra = 0f;
            if (!boostUsed && GameData.ExtraTimeCharges > 0)
            {
                GameData.ExtraTimeCharges--;
                GameData.Save();
                boostUsed = true;
            }
            if (boostUsed) extra = GameData.ExtraTimeSeconds;

            var ship = GameData.Ships[GameData.SelectedShip];
            int level = Mathf.Max(0, GameData.SelectedLevel);

            TotalTime = GameData.RunSeconds + ship.bonusSeconds + extra;
            TimeLeft = TotalTime;
            Score = 0;
            Lives = startLives;
            pointsPerComet = ship.points;
            spawnEvery = Mathf.Lerp(0.55f, 0.38f, level / 11f);
            fallSpeed = Mathf.Lerp(2.4f, 3.6f, level / 11f);
            spawnTimer = 0.4f;
            lastTick = 4;
            IsRunning = true;
            IsOver = false;
            DiedThisRun = false;
            ClearTargets();

            GameStarted?.Invoke();
        }

        // restart from the pause menu, same settings as before
        public void Restart()
        {
            BeginRun();
        }

        // quitting to the menu, just stop everything without raising won/lost
        public void Abort()
        {
            IsRunning = false;
            ClearTargets();
        }

        void Update()
        {
            if (!IsRunning || GamePause.IsPaused) return;

            TimeLeft -= Time.deltaTime;

            // tick sound for the last 3 seconds
            int whole = Mathf.CeilToInt(TimeLeft);
            if (whole <= 3 && whole >= 1 && whole < lastTick)
            {
                lastTick = whole;
                CountdownTick?.Invoke(whole);
            }

            if (TimeLeft <= 0f)
            {
                TimeLeft = 0f;
                EndRun();
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = spawnEvery * UnityEngine.Random.Range(0.7f, 1.3f);
                Spawn();
            }

            HandleTaps();
        }

        void Spawn()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            float roll = UnityEngine.Random.value;
            CometTarget template = cometTemplate;
            if (roll < goldChance) template = goldTemplate;
            else if (roll < goldChance + asteroidChance) template = asteroidTemplate;

            var pos = new Vector3(UnityEngine.Random.Range(-halfW + 0.6f, halfW - 0.6f), halfH + 1f, 0f);
            var target = Instantiate(template, pos, template.transform.rotation, spawnRoot);
            target.gameObject.SetActive(true);

            float speed = fallSpeed * UnityEngine.Random.Range(0.8f, 1.25f);
            if (target.kind == TargetKind.Gold) speed *= 1.3f; // gold ones are a bit faster
            target.Launch(new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), -speed, 0f), -halfH - 1.5f);
        }

        // works with the mouse in the editor and with touches on the phone
        void HandleTaps()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            var events = EventSystem.current;
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    if (touch.phase != TouchPhase.Began) continue;
                    if (events != null && events.IsPointerOverGameObject(touch.fingerId)) continue; // tapped a button
                    TryHit(touch.position);
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                if (events != null && events.IsPointerOverGameObject()) return;
                TryHit(Input.mousePosition);
            }
        }

        void TryHit(Vector2 screenPos)
        {
            Physics.SyncTransforms(); // the targets move by script, so update the colliders first
            var ray = cam.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;
            var target = hit.collider.GetComponentInParent<CometTarget>();
            if (target != null && IsRunning) HitTarget(target);
        }

        void HitTarget(CometTarget target)
        {
            Vector3 pos = target.transform.position;
            var kind = target.kind;
            Destroy(target.gameObject);

            switch (kind)
            {
                case TargetKind.Comet:
                    Score += pointsPerComet;
                    Collected?.Invoke(Score, pos);
                    break;

                case TargetKind.Gold:
                    Score += pointsPerComet * 2;
                    TimeLeft += goldBonusSeconds;
                    if (TimeLeft > 3f) lastTick = 4; // countdown can happen again if we got back above 3 sec
                    Collected?.Invoke(Score, pos);
                    TimeBonus?.Invoke(goldBonusSeconds, pos);
                    break;

                case TargetKind.Asteroid:
                    Lives--;
                    Damaged?.Invoke(Lives, pos);
                    if (Lives <= 0)
                    {
                        DiedThisRun = true;
                        Died?.Invoke(pos);
                        EndRun();
                    }
                    break;
            }
        }

        void EndRun()
        {
            if (!IsRunning) return;
            IsRunning = false;
            IsOver = true;

            // endless has no target score, any finished run counts as done
            int level = Mathf.Max(0, GameData.SelectedLevel);
            bool reachedGoal = GameData.Mode == GameMode.Endless || Score >= GameData.ScoreForGem(1, level);
            bool won = reachedGoal && !DiedThisRun;

            foreach (Transform child in spawnRoot)
            {
                var t = child.GetComponent<CometTarget>();
                if (t != null) t.Freeze();
            }

            if (won) GameWon?.Invoke(Score);
            else GameLost?.Invoke(Score);
        }

        void ClearTargets()
        {
            for (int i = spawnRoot.childCount - 1; i >= 0; i--)
                Destroy(spawnRoot.GetChild(i).gameObject);
        }
    }
}
