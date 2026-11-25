using CometRun.Audio;
using CometRun.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CometRun.UI
{
    // Goes on every button. It shrinks a bit when you press it and bounces
    // back when you let go, makes the buttons feel more like real buttons.
    //
    // Lab 8: it also plays the button sounds through UIAudio, so the buttons
    // don't need an AudioSource each. Hover only happens with a mouse (phones have no hover).
    public class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler,
        IPointerEnterHandler, IPointerClickHandler
    {
        public float pressedScale = 0.94f;
        [Tooltip("Normal size, the selected ship card uses 1.05")]
        public float restScale = 1f;

        [Tooltip("Sound when the button is pressed. None for buttons whose action already makes a sound (pause, resume, back)")]
        public UISound clickSound = UISound.Click;
        public bool hoverSound = true;

        Selectable selectable;
        bool pressed;

        bool CanUse => selectable == null || selectable.IsInteractable();

        void Awake()
        {
            selectable = GetComponent<Selectable>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // mouse pointers have negative ids, touches start at 0
            if (!hoverSound || eventData.pointerId >= 0 || !CanUse) return;
            UIAudio.Play(UISound.Hover);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!CanUse) return;
            UIAudio.Play(clickSound);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanUse) return;
            pressed = true;
            transform.ScaleTo(Vector3.one * restScale * pressedScale, 0.08f, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
        }

        void Release()
        {
            if (!pressed) return;
            pressed = false;
            transform.ScaleTo(Vector3.one * restScale, 0.25f, Ease.OutBack);
        }

        void OnDisable()
        {
            pressed = false;
            TweenRunner.Kill(transform, "scale");
            transform.localScale = Vector3.one * restScale;
        }
    }
}
