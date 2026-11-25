using UnityEngine;

namespace CometRun.Game
{
    // Listener #4: shakes the camera a bit when you hit an asteroid, and a lot when you die.
    // The camera lives in the menu scene, so I grab Camera.main and put it back after.
    public class CameraShake : MonoBehaviour
    {
        public GameSession session;
        public float hitStrength = 0.12f;
        public float hitTime = 0.25f;
        public float deathStrength = 0.3f;
        public float deathTime = 0.6f;

        Transform cam;
        Vector3 home;
        float timeLeft;
        float duration;
        float strength;

        void OnEnable()
        {
            session.Damaged += OnDamaged;
            session.Died += OnDied;
        }

        void OnDisable()
        {
            session.Damaged -= OnDamaged;
            session.Died -= OnDied;
            StopShake();
        }

        void OnDamaged(int livesLeft, Vector3 where) => StartShake(hitStrength, hitTime);
        void OnDied(Vector3 where) => StartShake(deathStrength, deathTime);

        void StartShake(float amount, float time)
        {
            if (cam == null)
            {
                if (Camera.main == null) return;
                cam = Camera.main.transform;
                home = cam.localPosition;
            }
            strength = Mathf.Max(strength * (timeLeft / Mathf.Max(0.01f, duration)), amount);
            duration = time;
            timeLeft = time;
        }

        void Update()
        {
            if (cam == null || timeLeft <= 0f) return;
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f)
            {
                StopShake();
                return;
            }
            float fade = timeLeft / duration; // gets weaker towards the end
            Vector2 offset = Random.insideUnitCircle * strength * fade;
            cam.localPosition = home + new Vector3(offset.x, offset.y, 0f);
        }

        void StopShake()
        {
            timeLeft = 0f;
            strength = 0f;
            if (cam != null) cam.localPosition = home;
        }
    }
}
