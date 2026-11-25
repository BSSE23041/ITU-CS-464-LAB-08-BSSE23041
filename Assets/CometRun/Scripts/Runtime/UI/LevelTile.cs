using UnityEngine;
using UnityEngine.UI;

namespace CometRun.UI
{
    // One level button in the grid. It can be cleared, next (orange) or locked.
    public class LevelTile : MonoBehaviour
    {
        public Button button;
        public Image background;
        public Text number;
        public Text lockText;
        public GameObject gemRow;
        public Image[] gems;

        public void SetState(int index, bool unlocked, bool isNext, int gemCount)
        {
            number.text = (index + 1).ToString();
            lockText.gameObject.SetActive(!unlocked);
            gemRow.SetActive(unlocked);

            if (!unlocked)
            {
                background.color = UIPalette.Locked;
                number.color = UIPalette.LockedText;
                return;
            }

            background.color = isNext ? UIPalette.Primary : UIPalette.SurfaceLight;
            number.color = isNext ? UIPalette.OnPrimary : UIPalette.Text;

            Color empty = isNext ? UIPalette.WithAlpha(UIPalette.OnPrimary, 0.35f) : UIPalette.GemEmpty;
            for (int i = 0; i < gems.Length; i++)
                gems[i].color = i < gemCount ? UIPalette.Gold : empty;
        }
    }
}
