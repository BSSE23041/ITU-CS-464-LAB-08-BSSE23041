using CometRun.Audio;
using CometRun.Data;
using CometRun.Tweening;
using CometRun.UI;
using UnityEngine;

namespace CometRun.Screens
{
    // 09 Shop. Spend coins on the other ships or Extra Time boosts.
    public class ShopScreen : UIScreen
    {
        public ShopItem[] items;
        public RectTransform[] entrance;

        protected override void Awake()
        {
            base.Awake();
            foreach (var item in items)
            {
                var target = item;
                item.buyButton.onClick.AddListener(() => Buy(target));
            }
        }

        public override void Refresh()
        {
            foreach (var item in items) item.Refresh();
        }

        public override void OnEnter()
        {
            PlayStagger(entrance, 0.07f);
        }

        void Buy(ShopItem item)
        {
            if (item.Owned) return;

            if (!GameData.TrySpend(item.Price))
            {
                item.BuyRect.Shake();
                Nav.ShowToast("You need " + (item.Price - GameData.Coins) + " more coins", true);
                return;
            }

            if (item.kind == ShopItemKind.Ship)
            {
                GameData.ShipOwned[item.shipIndex] = true;
                Nav.ShowToast(GameData.Ships[item.shipIndex].name + " unlocked! Equip it in the Hangar");
            }
            else
            {
                GameData.ExtraTimeCharges++;
                Nav.ShowToast("Extra Time added for your next run");
            }

            GameData.Save();
            UIAudio.Play(UISound.Success);
            item.BuyRect.Punch(0.12f, 0.3f);
            Refresh();
        }
    }
}
