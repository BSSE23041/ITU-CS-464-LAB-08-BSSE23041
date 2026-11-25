using CometRun.Audio;
using CometRun.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CometRun.UI
{
    // Every screen inherits from this. The ScreenManager decides when screens
    // show/hide, each screen just fills in its own text and does its own
    // little intro animation.
    [RequireComponent(typeof(CanvasGroup))]
    public class UIScreen : MonoBehaviour
    {
        [Tooltip("Tick this for popups like Pause that open on top of the current screen")]
        public bool isOverlay;
        [Tooltip("The panel that pops in for popups")]
        public RectTransform panel;
        [Tooltip("Back button in the top bar (can be empty)")]
        public Button backButton;

        CanvasGroup group;

        public CanvasGroup Group => group != null ? group : (group = GetComponent<CanvasGroup>());
        public RectTransform Rect => (RectTransform)transform;
        protected ScreenManager Nav => ScreenManager.Instance;

        protected virtual void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBack);
        }

        // update the texts/colours from GameData (no animation)
        public virtual void Refresh() { }

        // runs when the screen opens
        public virtual void OnEnter() { }

        // runs when you come back to this screen from another one
        public virtual void OnReturn() { }

        // back button in the top bar, phone back button and Escape all end up here
        public virtual void OnBack()
        {
            UIAudio.Play(UISound.Back);
            Nav.Back();
        }

        // things fade in one after another from top to bottom so you read them in order.
        // I scale instead of moving them because the layout group controls the positions
        // and moving them would fight with it.
        protected void PlayStagger(RectTransform[] items, float step = 0.06f, float startDelay = 0.08f)
        {
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item == null) continue;

                var itemGroup = item.GetComponent<CanvasGroup>();
                if (itemGroup == null) itemGroup = item.gameObject.AddComponent<CanvasGroup>();

                float delay = startDelay + i * step;
                item.localScale = Vector3.one * 0.92f;
                itemGroup.alpha = 0f;
                item.ScaleTo(Vector3.one, 0.4f, Ease.OutCubic).SetDelay(delay);
                itemGroup.FadeTo(1f, 0.3f, Ease.OutQuad).SetDelay(delay);
            }
        }

        // pops things in one by one with a small overshoot (used for level tiles)
        protected void PlayPopIn(Transform[] items, float step = 0.03f, float startDelay = 0.1f)
        {
            for (int i = 0; i < items.Length; i++)
            {
                items[i].localScale = Vector3.one * 0.6f;
                items[i].ScaleTo(Vector3.one, 0.3f, Ease.OutBack).SetDelay(startDelay + i * step);
            }
        }
    }
}
