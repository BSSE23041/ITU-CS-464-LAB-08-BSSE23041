using CometRun.Data;
using CometRun.UI;
using UnityEngine.UI;

namespace CometRun.Screens
{
    // 05 Hangar (I called it loadout in code). Pick your ship and start the run.
    public class LoadoutScreen : UIScreen
    {
        public ShipCard[] cards;
        public Button startButton;
        public Text infoText;
        public Text boostText;

        public UIScreen loading;
        public UIScreen shop;

        protected override void Awake()
        {
            base.Awake();
            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                cards[i].button.onClick.AddListener(() => OnCard(index));
            }
            // Lab 8: start goes to the loading screen first, it opens gameplay when the scene is ready
            startButton.onClick.AddListener(() => Nav.Push(loading));
        }

        public override void Refresh()
        {
            if (!GameData.ShipOwned[GameData.SelectedShip]) GameData.SelectedShip = 0;

            infoText.text = GameData.Mode == GameMode.Endless
                ? "Endless run"
                : GameData.ModeName(GameData.Mode) + "  |  Level " + (GameData.SelectedLevel + 1);

            boostText.text = GameData.ExtraTimeCharges > 0
                ? "Extra Time boosts: " + GameData.ExtraTimeCharges + "  (one is used this run)"
                : "Extra Time boosts: 0  (buy them in the Shop)";

            UpdateCards(false);
        }

        public override void OnReturn()
        {
            // if we came back from the shop the player might own a new ship now
            Refresh();
        }

        void UpdateCards(bool animate)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].Setup(GameData.Ships[i]);
                cards[i].SetState(GameData.ShipOwned[i], i == GameData.SelectedShip, animate);
            }
        }

        void OnCard(int index)
        {
            if (!GameData.ShipOwned[index])
            {
                Nav.ShowToast("Unlock " + GameData.Ships[index].name + " in the Shop");
                Nav.Push(shop);
                return;
            }
            GameData.SelectedShip = index;
            GameData.Save();
            UpdateCards(true);
        }
    }
}
