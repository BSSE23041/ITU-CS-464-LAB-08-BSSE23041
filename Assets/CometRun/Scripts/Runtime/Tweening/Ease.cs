using UnityEngine;

namespace CometRun.Tweening
{
    // The ease types I use for the menu animations. The maths is the usual Penner easing formulas.
    public enum Ease
    {
        Linear,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        InBack,
        OutBack,
        InOutSine
    }

    public static class EaseCurves
    {
        const float BackC1 = 1.70158f;
        const float BackC3 = BackC1 + 1f;

        public static float Evaluate(Ease ease, float t)
        {
            t = Mathf.Clamp01(t);
            switch (ease)
            {
                case Ease.InQuad: return t * t;
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                case Ease.InCubic: return t * t * t;
                case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.InOutCubic: return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
                case Ease.InBack: return BackC3 * t * t * t - BackC1 * t * t;
                case Ease.OutBack: return 1f + BackC3 * Mathf.Pow(t - 1f, 3f) + BackC1 * Mathf.Pow(t - 1f, 2f);
                case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                default: return t;
            }
        }
    }
}
