using CometRun.Data;
using CometRun.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CometRun.UI
{
    // One ship card on the hangar screen.
    public class ShipCard : MonoBehaviour
    {
        public Button button;
        public ButtonFeedback feedback;
        public Image border;
        public Image icon;
        public Text nameText;
        public RectTransform powerBar;
        public RectTransform timeBar;
        public Text powerValue;
        public Text timeValue;
        public Image tagBackground;
        public Text tagText;

        ShipInfo info;

        public void Setup(ShipInfo ship)
        {
            info = ship;
            nameText.text = ship.name;
            powerValue.text = ship.points.ToString();
            timeValue.text = "+" + ship.bonusSeconds + "s";
            powerBar.anchorMax = new Vector2(ship.points / 15f, 1f);
            timeBar.anchorMax = new Vector2((ship.bonusSeconds + 1) / 5f, 1f);
        }

        public void SetState(bool owned, bool selected, bool animate)
        {
            if (selected)
            {
                // selected one gets a white border, EQUIPPED and is a bit bigger (not only colour)
                border.color = UIPalette.Text;
                tagBackground.color = UIPalette.Primary;
                tagText.color = UIPalette.OnPrimary;
                tagText.text = "EQUIPPED";
            }
            else if (owned)
            {
                border.color = UIPalette.SurfaceLight;
                tagBackground.color = UIPalette.SurfaceLight;
                tagText.color = UIPalette.Text;
                tagText.text = "SELECT";
            }
            else
            {
                border.color = UIPalette.Surface;
                tagBackground.color = UIPalette.Locked;
                tagText.color = UIPalette.LockedText;
                tagText.text = "LOCKED";
            }

            icon.color = owned ? info.color : UIPalette.Disabled;
            nameText.color = owned ? UIPalette.Text : UIPalette.LockedText;

            float rest = selected ? 1.05f : 1f;
            feedback.restScale = rest;
            if (animate) transform.ScaleTo(Vector3.one * rest, 0.3f, Ease.OutBack);
            else transform.localScale = Vector3.one * rest;
        }
    }
}
