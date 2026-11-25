using System;
using System.Collections.Generic;
using CometRun.Audio;
using CometRun.Data;
using CometRun.Tweening;
using UnityEngine;

namespace CometRun.UI
{
    // Takes care of moving between screens. It keeps a list (stack) of the screens
    // you opened so back always goes to the one you really came from.
    //   Push    = go forward, new screen slides in from the right
    //   Back    = opposite of push, screen slides out to the right
    //   Overlay = popup like pause, background darkens and the panel pops up
    //   ResetTo = fade and clear the history (only used for title to main menu)
    //
    // Taps are blocked while a screen is animating, otherwise double tapping opened two screens.
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance { get; private set; }

        public UIScreen firstScreen;
        public UIScreen[] screens;
        public Toast toast;

        public const float SlideTime = 0.35f;
        public const float OverlayInTime = 0.25f;
        public const float OverlayOutTime = 0.2f;
        public const float FadeTime = 0.45f;
        const float Parallax = 0.3f;

        readonly List<UIScreen> stack = new List<UIScreen>();
        CanvasGroup rootGroup;
        bool busy;
        int waitId;

        public UIScreen Top => stack.Count > 0 ? stack[stack.Count - 1] : null;
        public bool Busy => busy;
        RectTransform Root => (RectTransform)transform;

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            rootGroup = GetComponent<CanvasGroup>();
            GamePause.ResetState();
            GameData.Load();
            foreach (var s in screens) s.gameObject.SetActive(false);
        }

        void Start()
        {
            stack.Add(firstScreen);
            Activate(firstScreen);
            firstScreen.Group.alpha = 0f;
            firstScreen.Group.FadeTo(1f, 0.5f, Ease.OutQuad);
            firstScreen.OnEnter();
        }

        void Update()
        {
            // on android the back button comes in as Escape
            if (Input.GetKeyDown(KeyCode.Escape) && !busy && Top != null)
                Top.OnBack();
        }

        public void ShowToast(string message, bool warning = false)
        {
            if (toast != null) toast.Show(message, warning);
        }

        // --- navigation ---

        public void Push(UIScreen next)
        {
            if (busy || next == null || next == Top) return;
            var leaving = stack.Count > 0 ? VisibleFrom(stack.Count - 1) : new List<UIScreen>();
            stack.Add(next);
            if (next.isOverlay) OpenOverlay(next);
            else SlideForward(next, leaving);
        }

        // swaps the current screen out instead of stacking (used for gameplay to results)
        public void Replace(UIScreen next)
        {
            if (busy || next == null || stack.Count == 0) return;
            var leaving = VisibleFrom(stack.Count - 1);
            stack.RemoveRange(stack.Count - leaving.Count, leaving.Count);
            stack.Add(next);
            SlideForward(next, leaving);
        }

        public void Back()
        {
            if (busy || stack.Count <= 1) return;
            BackTo(stack[stack.Count - 2]);
        }

        public bool BackTo(UIScreen target)
        {
            int index = stack.IndexOf(target);
            if (busy || index < 0 || index == stack.Count - 1) return false;

            var leaving = VisibleFrom(stack.Count - 1);
            stack.RemoveRange(index + 1, stack.Count - index - 1);
            var returning = VisibleFrom(index);
            leaving.RemoveAll(returning.Contains);

            if (leaving.Count == 1 && leaving[0].isOverlay) CloseOverlay(leaving[0]);
            else SlideBack(leaving, returning);
            return true;
        }

        public void ResetTo(UIScreen root)
        {
            if (busy || root == null) return;
            var leaving = stack.Count > 0 ? VisibleFrom(stack.Count - 1) : new List<UIScreen>();
            stack.Clear();
            stack.Add(root);

            Lock(true);
            Activate(root);
            root.Group.alpha = 0f;
            root.Group.FadeTo(1f, FadeTime, Ease.OutQuad);
            foreach (var s in leaving)
            {
                s.Group.FadeTo(0f, 0.3f, Ease.InQuad);
                s.transform.ScaleTo(Vector3.one * 1.08f, FadeTime, Ease.OutCubic);
            }
            root.OnEnter();

            After(FadeTime, () =>
            {
                foreach (var s in leaving) s.gameObject.SetActive(false);
                Lock(false);
            });
        }

        // --- animations ---

        void SlideForward(UIScreen next, List<UIScreen> leaving)
        {
            float w = Root.rect.width;
            Lock(true);

            Activate(next);
            next.Rect.anchoredPosition = new Vector2(w, 0f);
            next.Rect.MoveTo(Vector2.zero, SlideTime, Ease.OutCubic);

            foreach (var s in leaving)
            {
                s.Rect.MoveTo(new Vector2(-w * Parallax, 0f), SlideTime, Ease.OutCubic);
                s.Group.FadeTo(0f, SlideTime, Ease.OutQuad);
            }

            next.OnEnter();

            After(SlideTime, () =>
            {
                foreach (var s in leaving) s.gameObject.SetActive(false);
                Lock(false);
            });
        }

        void SlideBack(List<UIScreen> leaving, List<UIScreen> returning)
        {
            float w = Root.rect.width;
            Lock(true);

            foreach (var s in returning)
            {
                Activate(s);
                s.Rect.anchoredPosition = new Vector2(-w * Parallax, 0f);
                s.Group.alpha = 0f;
                s.Rect.MoveTo(Vector2.zero, SlideTime, Ease.OutCubic);
                s.Group.FadeTo(1f, SlideTime, Ease.OutQuad);
            }

            foreach (var s in leaving)
            {
                s.transform.SetAsLastSibling();
                s.Rect.MoveTo(new Vector2(w, 0f), SlideTime, Ease.OutCubic);
            }

            After(SlideTime, () =>
            {
                foreach (var s in leaving) s.gameObject.SetActive(false);
                Lock(false);
                foreach (var s in returning) s.OnReturn();
            });
        }

        void OpenOverlay(UIScreen overlay)
        {
            UIAudio.Play(UISound.PanelOpen);
            Lock(true);
            Activate(overlay);
            overlay.Group.alpha = 0f;
            overlay.Group.FadeTo(1f, 0.2f, Ease.OutQuad);
            if (overlay.panel != null)
            {
                overlay.panel.localScale = Vector3.one * 0.85f;
                overlay.panel.ScaleTo(Vector3.one, OverlayInTime, Ease.OutBack);
            }
            overlay.OnEnter();
            After(OverlayInTime, () => Lock(false));
        }

        void CloseOverlay(UIScreen overlay)
        {
            UIAudio.Play(UISound.PanelClose);
            Lock(true);
            overlay.Group.FadeTo(0f, OverlayOutTime, Ease.InQuad);
            if (overlay.panel != null)
                overlay.panel.ScaleTo(Vector3.one * 0.9f, OverlayOutTime, Ease.InBack);

            After(OverlayOutTime, () =>
            {
                overlay.gameObject.SetActive(false);
                Lock(false);
                if (Top != null) Top.OnReturn();
            });
        }

        // --- helpers ---

        // the top screen, plus the one under it if the top one is a popup (both are visible)
        List<UIScreen> VisibleFrom(int index)
        {
            var list = new List<UIScreen>();
            for (int i = index; i >= 0; i--)
            {
                list.Insert(0, stack[i]);
                if (!stack[i].isOverlay) break;
            }
            return list;
        }

        void Activate(UIScreen s)
        {
            s.gameObject.SetActive(true);
            s.transform.SetAsLastSibling();
            s.Rect.anchoredPosition = Vector2.zero;
            s.transform.localScale = Vector3.one;
            s.Group.alpha = 1f;
            s.Refresh();
        }

        void Lock(bool locked)
        {
            busy = locked;
            rootGroup.blocksRaycasts = !locked;
        }

        void After(float seconds, Action action)
        {
            waitId++;
            Tweens.Wait(this, "wait" + waitId, seconds).OnComplete(action);
        }
    }
}
