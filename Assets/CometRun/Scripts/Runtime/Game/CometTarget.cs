using UnityEngine;

namespace CometRun.Game
{
    public enum TargetKind { Comet, Asteroid, Gold }

    // Something falling down the screen (comet, gold comet or asteroid).
    // It's just a Unity primitive with a collider so the tap raycast can hit it.
    public class CometTarget : MonoBehaviour
    {
        public TargetKind kind;
        public float spinSpeed = 90f;

        Vector3 velocity;
        float killY;
        bool frozen;

        public void Launch(Vector3 startVelocity, float removeBelowY)
        {
            velocity = startVelocity;
            killY = removeBelowY;
            frozen = false;
            if (kind == TargetKind.Asteroid) spinSpeed *= Random.Range(0.6f, 1.6f) * (Random.value < 0.5f ? -1f : 1f);
        }

        // when the run ends everything just stops where it is
        public void Freeze()
        {
            frozen = true;
        }

        void Update()
        {
            if (frozen) return;
            transform.position += velocity * Time.deltaTime;
            if (kind == TargetKind.Asteroid) transform.Rotate(spinSpeed * Time.deltaTime, spinSpeed * 0.7f * Time.deltaTime, 0f, Space.World);
            if (transform.position.y < killY) Destroy(gameObject); // missed it, no penalty
        }
    }
}
