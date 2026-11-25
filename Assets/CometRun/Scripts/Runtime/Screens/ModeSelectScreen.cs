using CometRun.Data;
using CometRun.Tweening;
using CometRun.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CometRun.Screens
{
    // 03 Mode select. Classic and Time Attack go to the level grid,
    // Endless has no levels so it skips straight to the hangar (it's locked at first though).
    public class ModeSelectScreen : UIScreen
    {
        public Button classicButton;
        public Button timeAttackButton;
        public Button endlessButton;
        public Image endlessBackground;
        public Image endlessAccent;
        public Text endlessTitle;
        public Text endlessDesc;
        public GameObject endlessLockTag;
        public RectTransform[] entrance;

        public UIScreen levelSelect;
        public UIScreen loadout;

        protected override void Awake()
        {
            base.Awake();
            classicButton.onClick.AddListener(() => Pick(GameMode.Classic));
            timeAttackButton.onClick.AddListener(() => Pick(GameMode.TimeAttack));
            endlessButton.onClick.AddListener(OnEndless);
        }

        public override void Refresh()
        {
            bool open = GameData.EndlessUnlocked;
            endlessLockTag.SetActive(!open);
            endlessBackground.color = open ? UIPalette.Surface : UIPalette.Locked;
            endlessAccent.color = open ? UIPalette.Accent : UIPalette.Disabled;
            endlessTitle.color = open ? UIPalette.Text : UIPalette.LockedText;
            endlessDesc.text = open
                ? "No level goals. 20 seconds to set your best score."
                : "Clear level " + GameData.EndlessUnlockLevel + " in Classic to unlock.";
        }

        public override void OnEnter()
        {
            PlayStagger(entrance, 0.07f);
        }

        void Pick(GameMode mode)
        {
            GameData.Mode = mode;
            Nav.Push(levelSelect);
        }

        void OnEndless()
        {
            if (!GameData.EndlessUnlocked)
            {
                ((RectTransform)endlessButton.transform).Shake();
                Nav.ShowToast("Endless unlocks after level " + GameData.EndlessUnlockLevel, true);
                return;
            }
            GameData.Mode = GameMode.Endless;
            GameData.SelectedLevel = -1;
            Nav.Push(loadout);
        }
    }
}
