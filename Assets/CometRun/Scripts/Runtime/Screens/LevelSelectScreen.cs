using CometRun.Data;
using CometRun.Tweening;
using CometRun.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CometRun.Screens
{
    // 04 Level select, 12 levels in a 3x4 grid
    public class LevelSelectScreen : UIScreen
    {
        public LevelTile[] tiles;
        public Text modeLabel;
        public UIScreen loadout;

        protected override void Awake()
        {
            base.Awake();
            for (int i = 0; i < tiles.Length; i++)
            {
                int index = i;
                tiles[i].button.onClick.AddListener(() => OnTile(index));
            }
        }

        public override void Refresh()
        {
            modeLabel.text = GameData.ModeName(GameData.Mode) + "  |  " + GameData.RunSeconds + " seconds per run";
            for (int i = 0; i < tiles.Length; i++)
            {
                bool unlocked = i < GameData.UnlockedLevels;
                bool isNext = i == GameData.UnlockedLevels - 1;
                tiles[i].SetState(i, unlocked, isNext, GameData.Gems[i]);
            }
        }

        public override void OnEnter()
        {
            var items = new Transform[tiles.Length];
            for (int i = 0; i < tiles.Length; i++) items[i] = tiles[i].transform;
            PlayPopIn(items);
        }

        void OnTile(int index)
        {
            if (index >= GameData.UnlockedLevels)
            {
                ((RectTransform)tiles[index].transform).Shake();
                Nav.ShowToast("Locked. Clear level " + GameData.UnlockedLevels + " first", true);
                return;
            }
            GameData.SelectedLevel = index;
            Nav.Push(loadout);
        }
    }
}
