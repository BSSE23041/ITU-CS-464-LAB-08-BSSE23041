using CometRun.Data;
using UnityEngine;
using UnityEngine.UI;

namespace CometRun.UI
{
    public enum ShopItemKind { Ship, ExtraTime }

    // One row in the shop. Buy button looks different depending on
    // if you can afford it (orange), can't (grey with red price) or already own it.
    public class ShopItem : MonoBehaviour
    {
        public ShopItemKind kind;
        public int shipIndex;
        public Text nameText;
        public Text descText;
        public Button buyButton;
        public Image buyBackground;
        public Text priceText;

        public RectTransform BuyRect => (RectTransform)buyButton.transform;
        public int Price => kind == ShopItemKind.Ship ? GameData.Ships[shipIndex].price : GameData.ExtraTimePrice;
        public bool Owned => kind == ShopItemKind.Ship && GameData.ShipOwned[shipIndex];

        public void Refresh()
        {
            if (kind == ShopItemKind.ExtraTime)
                descText.text = "+" + GameData.ExtraTimeSeconds + " seconds on your next run.\nYou have: " + GameData.ExtraTimeCharges;

            if (Owned)
            {
                buyButton.interactable = false;
                buyBackground.color = UIPalette.Surface;
                priceText.color = UIPalette.TextMuted;
                priceText.text = "OWNED";
                return;
            }

            buyButton.interactable = true;
            bool canAfford = GameData.Coins >= Price;
            buyBackground.color = canAfford ? UIPalette.Primary : UIPalette.SurfaceLight;
            priceText.color = canAfford ? UIPalette.OnPrimary : UIPalette.Danger;
            priceText.text = Price + "\n<size=26>COINS</size>";
        }
    }
}
