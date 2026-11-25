using CometRun.UI;
using UnityEngine;

namespace CometRun.Game
{
    // Listener #3: little bursts of particles where you tapped something.
    // One particle system for everything, I just Emit() at the right spot with a colour.
    public class HitParticles : MonoBehaviour
    {
        public GameSession session;
        public ParticleSystem particles;

        void OnEnable()
        {
            session.Collected += OnCollected;
            session.TimeBonus += OnTimeBonus;
            session.Damaged += OnDamaged;
            session.Died += OnDied;
        }

        void OnDisable()
        {
            session.Collected -= OnCollected;
            session.TimeBonus -= OnTimeBonus;
            session.Damaged -= OnDamaged;
            session.Died -= OnDied;
        }

        void OnCollected(int score, Vector3 where) => Burst(where, UIPalette.Primary, 14);
        void OnTimeBonus(float seconds, Vector3 where) => Burst(where, UIPalette.Gold, 22);
        void OnDamaged(int livesLeft, Vector3 where) => Burst(where, UIPalette.Danger, 18);
        void OnDied(Vector3 where) => Burst(where, UIPalette.Text, 40);

        void Burst(Vector3 where, Color color, int count)
        {
            var p = new ParticleSystem.EmitParams
            {
                position = where,
                startColor = color,
                applyShapeToPosition = true
            };
            particles.Emit(p, count);
        }
    }
}
