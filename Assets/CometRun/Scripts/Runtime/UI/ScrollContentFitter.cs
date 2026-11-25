using UnityEngine;
using UnityEngine.UI;

namespace CometRun.UI
{
    // Goes on the Content of each scroll view. Makes the content at least as tall as
    // the screen, or taller if the items need more space.
    // On a tall phone the spacers spread everything out to fill the screen,
    // on a short one it just becomes scrollable. Before this the text was
    // overlapping the buttons on small screens.
    [RequireComponent(typeof(RectTransform))]
    public class ScrollContentFitter : MonoBehaviour
    {
        public RectTransform viewport;

        void OnEnable()
        {
            Fit();
        }

        void LateUpdate()
        {
            Fit();
        }

        public void Fit()
        {
            if (viewport == null) return;
            var rt = (RectTransform)transform;
            float needed = LayoutUtility.GetPreferredHeight(rt);
            float height = Mathf.Max(viewport.rect.height, needed);
            if (Mathf.Abs(rt.rect.height - height) > 0.5f)
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }
    }
}
