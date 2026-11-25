using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CometRun.Tweening
{
    // Shortcuts so in the screens I can just write button.MoveTo(pos, 0.3f, Ease.OutCubic)
    public static class Tweens
    {
        public static Tween Value(Object owner, string channel, float from, float to, float duration, Ease ease, Action<float> onValue)
        {
            return TweenRunner.Run(owner, channel, duration, ease, k => onValue(Mathf.LerpUnclamped(from, to, k)));
        }

        public static Tween Wait(Object owner, string channel, float seconds)
        {
            return TweenRunner.Run(owner, channel, seconds, Ease.Linear, _ => { });
        }

        public static Tween MoveTo(this RectTransform rt, Vector2 to, float duration, Ease ease)
        {
            Vector2 from = rt.anchoredPosition;
            return TweenRunner.Run(rt, "move", duration, ease, k => rt.anchoredPosition = Vector2.LerpUnclamped(from, to, k));
        }

        public static Tween ScaleTo(this Transform tr, Vector3 to, float duration, Ease ease)
        {
            Vector3 from = tr.localScale;
            return TweenRunner.Run(tr, "scale", duration, ease, k => tr.localScale = Vector3.LerpUnclamped(from, to, k));
        }

        public static Tween FadeTo(this CanvasGroup group, float to, float duration, Ease ease)
        {
            float from = group.alpha;
            return TweenRunner.Run(group, "fade", duration, ease, k => group.alpha = Mathf.LerpUnclamped(from, to, k));
        }

        public static Tween ColorTo(this Graphic graphic, Color to, float duration, Ease ease)
        {
            Color from = graphic.color;
            return TweenRunner.Run(graphic, "color", duration, ease, k => graphic.color = Color.LerpUnclamped(from, to, k));
        }

        // quick little bump when something changes (like coins after buying)
        public static Tween Punch(this Transform tr, float amount = 0.15f, float duration = 0.3f)
        {
            return TweenRunner.Run(tr, "scale", duration, Ease.Linear,
                k => tr.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * amount));
        }

        // left-right shake that means "no", for locked stuff. It remembers the start
        // position so tapping fast doesn't make the thing drift away
        static readonly Dictionary<RectTransform, Vector2> shakeHome = new Dictionary<RectTransform, Vector2>();

        public static Tween Shake(this RectTransform rt, float strength = 22f, float duration = 0.4f)
        {
            if (!shakeHome.TryGetValue(rt, out Vector2 home))
            {
                home = rt.anchoredPosition;
                shakeHome[rt] = home;
            }

            return TweenRunner.Run(rt, "move", duration, Ease.Linear, k =>
            {
                float offset = Mathf.Sin(k * Mathf.PI * 8f) * strength * (1f - k);
                rt.anchoredPosition = home + new Vector2(offset, 0f);
            }).OnComplete(() => shakeHome.Remove(rt));
        }
    }
}
