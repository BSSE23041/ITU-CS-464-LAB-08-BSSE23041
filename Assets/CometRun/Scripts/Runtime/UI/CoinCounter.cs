using CometRun.Data;
using CometRun.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CometRun.UI
{
    // The coin thing in the top bar. When coins change the number counts
    // up/down to the new value and the pill bumps a little so you actually
    // notice it changed.
    public class CoinCounter : MonoBehaviour
    {
        public Text label;
        public Transform pill;

        int shown;

        void OnEnable()
        {
            GameData.CoinsChanged += OnCoinsChanged;
            SetImmediate(GameData.Coins);
        }

        void OnDisable()
        {
            GameData.CoinsChanged -= OnCoinsChanged;
        }

        public void SetImmediate(int value)
        {
            shown = value;
            label.text = value.ToString();
        }

        void OnCoinsChanged()
        {
            int from = shown;
            int to = GameData.Coins;
            shown = to;
            Tweens.Value(this, "count", from, to, 0.5f, Ease.OutQuad, v => label.text = Mathf.RoundToInt(v).ToString());
            if (pill != null) pill.Punch(to >= from ? 0.12f : -0.08f, 0.3f);
        }
    }
}
