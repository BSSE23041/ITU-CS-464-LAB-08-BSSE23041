using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CometRun.Tweening
{
    // One running animation. Each one has a target and a "channel" (move, scale, fade...).
    // If a new one starts on the same target + channel the old one gets stopped,
    // otherwise spamming a button stacked the animations and it looked broken.
    public class Tween
    {
        internal Object target;
        internal string channel;
        internal float duration;
        internal float delay;
        internal float elapsed;
        internal Ease ease;
        internal bool pingPong;
        internal bool killed;
        internal Action<float> setter;
        internal Action completed;

        public Tween SetDelay(float seconds) { delay = seconds; return this; }
        public Tween SetPingPong() { pingPong = true; return this; }
        public Tween OnComplete(Action action) { completed += action; return this; }
        public void Kill() { killed = true; }
    }

    // My own small tween system so I didn't need DOTween or LeanTween.
    // It uses unscaled time so the menus still animate even if timeScale is 0 (paused).
    public class TweenRunner : MonoBehaviour
    {
        static TweenRunner instance;
        readonly List<Tween> active = new List<Tween>();
        readonly List<Tween> added = new List<Tween>();

        static TweenRunner Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("TweenRunner");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<TweenRunner>();
                }
                return instance;
            }
        }

        public static Tween Run(Object target, string channel, float duration, Ease ease, Action<float> setter)
        {
            var tween = new Tween
            {
                target = target,
                channel = channel,
                duration = Mathf.Max(0.0001f, duration),
                ease = ease,
                setter = setter
            };

            if (!Application.isPlaying)
            {
                // not in play mode (e.g. called from an editor script) so just jump to the end
                setter(1f);
                tween.killed = true;
                return tween;
            }

            Kill(target, channel);
            Instance.added.Add(tween);
            return tween;
        }

        public static void Kill(Object target, string channel = null)
        {
            if (instance == null) return;
            KillIn(instance.active, target, channel);
            KillIn(instance.added, target, channel);
        }

        static void KillIn(List<Tween> list, Object target, string channel)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].target == target && (channel == null || list[i].channel == channel))
                    list[i].killed = true;
            }
        }

        void Update()
        {
            if (added.Count > 0)
            {
                active.AddRange(added);
                added.Clear();
            }

            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < active.Count; i++)
            {
                var t = active[i];
                if (t.killed || t.target == null) continue;

                if (t.delay > 0f)
                {
                    t.delay -= dt;
                    continue;
                }

                t.elapsed += dt;
                float p = t.elapsed / t.duration;

                if (t.pingPong)
                {
                    t.setter(EaseCurves.Evaluate(t.ease, Mathf.PingPong(p, 1f)));
                    continue;
                }

                if (p >= 1f)
                {
                    t.setter(1f);
                    t.killed = true;
                    t.completed?.Invoke();
                }
                else
                {
                    t.setter(EaseCurves.Evaluate(t.ease, p));
                }
            }

            active.RemoveAll(t => t.killed || t.target == null);
        }
    }
}
