using System.IO;
using CometRun.Audio;
using CometRun.Screens;
using CometRun.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static CometRun.EditorTools.UIFactory;

namespace CometRun.EditorTools
{
    // I build the whole Main.unity scene from code here because placing 10 screens
    // by hand kept breaking every time I changed something. If the scene gets messed
    // up just run Comet Run > Build Menu Scene and it makes it again.
    //
    // Every screen is set up the same way:
    //   TopBar = stuck to the top (back button, title, coins)
    //   Scroll = scroll view for everything below it
    //     Content has a vertical layout group so things stack and don't overlap.
    //     The spacers stretch on tall phones and shrink on short ones (then it just scrolls).
    // Canvas scaler matches width (1080) so nothing gets cut off on the sides.
    //
    // Lab 8 added: the loading screen (11), the audio objects (mixer volumes, music, UI sounds),
    // the real gameplay HUD and four volume sliders in settings.
    public static class MenuSceneBuilder
    {
        public const string ScenePath = "Assets/CometRun/Scenes/Main.unity";
        const float TopBarHeight = 180f;

        // builds both scenes in the right order
        [MenuItem("Comet Run/Build All Scenes")]
        public static void BuildAll()
        {
            GameSceneBuilder.Build();
            Build();
        }

        [MenuItem("Comet Run/Build Menu Scene")]
        public static void Build()
        {
            LoadResources();
            Directory.CreateDirectory("Assets/CometRun/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UIPalette.Background;
            cam.orthographic = true;
            cam.orthographicSize = 5f; // the gameplay scene is laid out for this size
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();

            BuildAudio();

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f; // match width so it's always 1080 wide, height can scroll

            // no full screen background image on the canvas anymore, the camera already clears to
            // the same navy colour and the game objects need to be visible behind the gameplay HUD

            var safe = New("SafeArea", canvasGo.transform);
            Stretch(safe);
            safe.AddComponent<SafeArea>();

            var screensGo = New("Screens", safe.transform);
            Stretch(screensGo);
            screensGo.AddComponent<CanvasGroup>();
            var manager = screensGo.AddComponent<ScreenManager>();

            var title = CreateScreen<TitleScreen>(screensGo.transform, "01_Title");
            var main = CreateScreen<MainMenuScreen>(screensGo.transform, "02_MainMenu");
            var mode = CreateScreen<ModeSelectScreen>(screensGo.transform, "03_ModeSelect");
            var level = CreateScreen<LevelSelectScreen>(screensGo.transform, "04_LevelSelect");
            var loadout = CreateScreen<LoadoutScreen>(screensGo.transform, "05_Loadout");
            var gameplay = CreateScreen<GameplayScreen>(screensGo.transform, "06_Gameplay");
            var pause = CreateScreen<PauseScreen>(screensGo.transform, "07_Pause", true);
            var results = CreateScreen<ResultsScreen>(screensGo.transform, "08_Results");
            var shop = CreateScreen<ShopScreen>(screensGo.transform, "09_Shop");
            var settings = CreateScreen<SettingsScreen>(screensGo.transform, "10_Settings");
            var loading = CreateScreen<LoadingScreen>(screensGo.transform, "11_Loading");

            BuildTitle(title);
            BuildMainMenu(main);
            BuildModeSelect(mode);
            BuildLevelSelect(level);
            BuildLoadout(loadout);
            BuildGameplay(gameplay);
            BuildPause(pause);
            BuildResults(results);
            BuildShop(shop);
            BuildSettings(settings);
            BuildLoading(loading);

            // hook up which button goes to which screen
            title.mainMenu = main;
            main.modeSelect = mode; main.shop = shop; main.settings = settings; main.title = title;
            mode.levelSelect = level; mode.loadout = loadout;
            level.loadout = loadout;
            loadout.loading = loading; loadout.shop = shop;
            loading.gameplay = gameplay;
            gameplay.pause = pause; gameplay.results = results;
            pause.gameplay = gameplay; pause.settings = settings; pause.mainMenu = main;
            results.loading = loading; results.loadout = loadout; results.levelSelect = level; results.mainMenu = main;

            manager.firstScreen = title;
            manager.screens = new UIScreen[] { title, main, mode, level, loadout, gameplay, pause, results, shop, settings, loading };
            manager.toast = BuildToast(safe.transform);

            foreach (var s in manager.screens) s.gameObject.SetActive(s == title); // only title is on at the start

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(GameSceneBuilder.ScenePath, true), // loaded by the loading screen
            };

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.productName = "Comet Run";
            AssetDatabase.SaveAssets();
            Debug.Log("Comet Run: menu scene built at " + ScenePath);
        }

        // ===== stuff every screen uses =====

        static T CreateScreen<T>(Transform parent, string name, bool overlay = false) where T : UIScreen
        {
            var go = New(name, parent);
            Stretch(go);
            var bg = go.AddComponent<Image>();
            bg.color = overlay ? new Color(0, 0, 0, 0.72f) : UIPalette.Background;
            go.AddComponent<CanvasGroup>();
            var screen = go.AddComponent<T>();
            screen.isOverlay = overlay;
            return screen;
        }

        // scroll view under the top bar. Gives back the Content object so I can add things to it
        static Transform Body(UIScreen screen, bool underTopBar, out Image viewportImage)
        {
            var scrollGo = New("Scroll", screen.transform);
            Stretch(scrollGo, 0, 0, underTopBar ? TopBarHeight : 0, 0);

            viewportImage = Img(scrollGo.transform, "Viewport", new Color(0, 0, 0, 0), null, true);
            Stretch(viewportImage);
            viewportImage.gameObject.AddComponent<RectMask2D>();

            var content = New("Content", viewportImage.transform);
            var crt = Rt(content);
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(0, 1500);

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 30, 60);
            layout.spacing = 24;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = content.AddComponent<ScrollContentFitter>();
            fitter.viewport = Rt(viewportImage);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.content = crt;
            scroll.viewport = Rt(viewportImage);
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.1f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;
            scroll.scrollSensitivity = 40f;
            scroll.verticalScrollbar = MakeScrollbar(scrollGo.transform);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.verticalScrollbarSpacing = 0;

            return content.transform;
        }

        static Transform Body(UIScreen screen, bool underTopBar) => Body(screen, underTopBar, out _);

        // thin scrollbar on the right, it hides itself when there is nothing to scroll
        static Scrollbar MakeScrollbar(Transform parent)
        {
            var track = Img(parent, "Scrollbar", UIPalette.WithAlpha(UIPalette.SurfaceLight, 0.35f), Rounded, true);
            var tr = Rt(track);
            tr.anchorMin = new Vector2(1, 0);
            tr.anchorMax = new Vector2(1, 1);
            tr.pivot = new Vector2(1, 0.5f);
            tr.anchoredPosition = new Vector2(-6, 0);
            tr.sizeDelta = new Vector2(12, -40);

            var area = New("Sliding Area", track.transform);
            Stretch(area);
            var handle = Img(area.transform, "Handle", UIPalette.WithAlpha(UIPalette.TextMuted, 0.8f), Rounded, true);
            Rt(handle).sizeDelta = Vector2.zero;

            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = Rt(handle);
            bar.targetGraphic = handle;
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.navigation = new Navigation { mode = Navigation.Mode.None };
            return bar;
        }

        // gives something a fixed size inside the vertical layout
        static T Item<T>(T c, float w, float h, float flexible = 0f) where T : Component
        {
            var rt = Rt(c);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.minHeight = h;
            le.preferredHeight = h;
            le.flexibleHeight = flexible;
            return c;
        }

        static RectTransform Group(Transform content, string name, float w, float h, float flexible = 0f)
        {
            var go = New(name, content);
            return Item(Rt(go), w, h, flexible);
        }

        // empty gap. Grows on tall phones, shrinks down to minHeight on short ones
        static void Spacer(Transform content, float minHeight, float flexible = 1f)
        {
            var go = New("Spacer", content);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = minHeight;
            le.preferredHeight = minHeight;
            le.flexibleHeight = flexible;
            Rt(go).sizeDelta = new Vector2(10, minHeight);
        }

        static Text Caption(Transform content, string name, string text, int size = 36, float h = 60)
        {
            var t = Txt(content, name, text, size, UIPalette.TextMuted);
            return Item(t, 960, h);
        }

        static Button Btn(Transform parent, string name, string label, ButtonStyle style, float w, float h, int fontSize)
        {
            var b = MakeButton(parent, name, label, style, 0.5f, 0.5f, 0, 0, w, h, fontSize);
            return Item(b, w, h);
        }

        static RectTransform TopBar(UIScreen screen, string title, bool back, bool coins)
        {
            var bar = New("TopBar", screen.transform);
            var rt = Rt(bar);
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0, TopBarHeight);

            if (back)
            {
                screen.backButton = IconButton(bar.transform, "BackButton", 0, 0.5f, 110, 0, true);
                SetClickSound(screen.backButton, UISound.None); // OnBack plays the back sound
            }

            var t = Txt(bar.transform, "Title", title, 60, UIPalette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(t, 0.5f, 0.5f, 0, 0, 560, 100);

            if (coins) CoinPill(bar.transform);
            return rt;
        }

        static Button IconButton(Transform parent, string name, float ax, float ay, float x, float y, bool chevron)
        {
            var root = Bordered(parent, name, UIPalette.SurfaceLight, UIPalette.Surface, 5f, out var fill, true);
            Place(root, ax, ay, x, y, 140, 140);
            if (chevron)
            {
                Bar(root.transform, "ChevronTop", UIPalette.Text, -4, 13, 44, 11, 45);
                Bar(root.transform, "ChevronBottom", UIPalette.Text, -4, -13, 44, 11, -45);
            }
            else
            {
                Bar(root.transform, "BarLeft", UIPalette.Text, -15, 0, 16, 56, 0);
                Bar(root.transform, "BarRight", UIPalette.Text, 15, 0, 16, 56, 0);
            }
            var button = root.gameObject.AddComponent<Button>();
            SetupButton(button, fill);
            return button;
        }

        static void CoinPill(Transform parent)
        {
            var pill = Img(parent, "CoinPill", UIPalette.Surface, Rounded);
            Place(pill, 1, 0.5f, -170, 0, 260, 100);
            var icon = Img(pill.transform, "CoinIcon", UIPalette.Gold, Circle);
            Place(icon, 0, 0.5f, 52, 0, 54, 54);
            var amount = Txt(pill.transform, "Amount", "0", 46, UIPalette.Text, TextAnchor.MiddleRight, FontStyle.Bold);
            Stretch(amount, 90, 28, 0, 0);
            var counter = pill.gameObject.AddComponent<CoinCounter>();
            counter.label = amount;
            counter.pill = pill.transform;
        }

        // ===== 01 Title =====
        static void BuildTitle(TitleScreen s)
        {
            var t = s.transform;
            Stars(t, 40, 7);
            var planet = Img(t, "Planet", UIPalette.Surface, Circle);
            Place(planet, 0.5f, 0, 0, -420, 1600, 1600);

            var c = Body(s, false, out var viewport);

            Spacer(c, 80, 1f);
            var logo = Group(c, "Logo", 900, 440);
            var trail3 = Img(logo, "Trail3", UIPalette.WithAlpha(UIPalette.Primary, 0.2f), Circle);
            Place(trail3, 0.5f, 0.5f, 215, 165, 28, 28);
            var trail2 = Img(logo, "Trail2", UIPalette.WithAlpha(UIPalette.Primary, 0.4f), Circle);
            Place(trail2, 0.5f, 0.5f, 260, 190, 44, 44);
            var trail1 = Img(logo, "Trail1", UIPalette.WithAlpha(UIPalette.Primary, 0.65f), Circle);
            Place(trail1, 0.5f, 0.5f, 315, 220, 62, 62);
            var head = Img(logo, "CometHead", UIPalette.Primary, Circle);
            Place(head, 0.5f, 0.5f, 385, 255, 90, 90);
            var l1 = Txt(logo, "Word1", "COMET", 190, UIPalette.Primary, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(l1, 0.5f, 0.5f, 0, 80, 900, 210);
            var l2 = Txt(logo, "Word2", "RUN", 190, UIPalette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(l2, 0.5f, 0.5f, 0, -110, 900, 210);

            Caption(c, "Tagline", "Tap the comets. Beat the clock.", 46, 80);
            Spacer(c, 80, 1.2f);

            var hint = Group(c, "TapHint", 960, 110);
            var hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
            hintGroup.blocksRaycasts = false;
            var hintText = Txt(hint, "Label", "TAP ANYWHERE TO START", 54, UIPalette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(hintText);

            Spacer(c, 60, 0.5f);
            Caption(c, "Version", "v1.1  |  ITU CS-464 Lab 8", 32);

            // I put the "tap anywhere" button on the scroll viewport itself, that way
            // dragging still scrolls but a normal tap goes to the menu
            var tapButton = viewport.gameObject.AddComponent<Button>();
            tapButton.transition = Selectable.Transition.None;
            tapButton.navigation = new Navigation { mode = Navigation.Mode.None };

            s.tapArea = tapButton;
            s.logo = logo;
            s.tapHint = hintGroup;
        }

        // ===== 02 Main menu =====
        static void BuildMainMenu(MainMenuScreen s)
        {
            TopBar(s, "", false, true);
            var c = Body(s, true);

            Spacer(c, 30, 0.6f);
            var logo = Item(Txt(c, "Logo", "<color=#F59E0B>COMET</color> RUN", 124, UIPalette.Text, TextAnchor.MiddleCenter, FontStyle.Bold), 1000, 160);
            var sub = Caption(c, "Subtitle", "Main Menu", 42);
            Spacer(c, 60, 1f);

            s.playButton = Btn(c, "PlayButton", "PLAY", ButtonStyle.Primary, 760, 210, 80);
            var row = Group(c, "ButtonRow", 760, 150);
            s.shopButton = MakeButton(row, "ShopButton", "SHOP", ButtonStyle.Secondary, 0.5f, 0.5f, -195, 0, 370, 150, 50);
            s.settingsButton = MakeButton(row, "SettingsButton", "SETTINGS", ButtonStyle.Secondary, 0.5f, 0.5f, 195, 0, 370, 150, 50);
            s.progressText = Caption(c, "Progress", "Levels cleared: 0 / 12", 40);

            Spacer(c, 60, 1.4f);
            s.quitButton = Btn(c, "QuitButton", "Quit", ButtonStyle.Ghost, 400, 130, 46);

            s.entrance = new[] { Rt(logo), Rt(sub), Rt(s.playButton), row, Rt(s.progressText), Rt(s.quitButton) };
        }

        // ===== 03 Mode select =====
        static void BuildModeSelect(ModeSelectScreen s)
        {
            TopBar(s, "SELECT MODE", true, true);
            var c = Body(s, true);

            s.classicButton = ModeCard(c, "ClassicCard", "CLASSIC", "Clear 12 levels and earn up to 3 gems on each one.",
                "15 seconds per run", UIPalette.Primary, out _, out _, out _, out _);
            s.timeAttackButton = ModeCard(c, "TimeAttackCard", "TIME ATTACK", "Same levels with only 10 seconds. Tap faster!",
                "10 seconds per run", UIPalette.Secondary, out _, out _, out _, out _);
            s.endlessButton = ModeCard(c, "EndlessCard", "ENDLESS", "Clear level 6 in Classic to unlock.",
                "20 seconds  |  high score", UIPalette.Accent, out s.endlessBackground, out s.endlessAccent, out s.endlessTitle, out s.endlessDesc);

            var tag = Img(s.endlessButton.transform, "LockTag", UIPalette.Background, Rounded);
            Place(tag, 1, 1, -140, -70, 200, 70);
            var tagText = Txt(tag.transform, "Label", "LOCKED", 32, UIPalette.LockedText, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(tagText);
            s.endlessLockTag = tag.gameObject;

            Spacer(c, 30, 1f);
            var hint = Caption(c, "Hint", "Faster modes give the same gems with less time", 34);

            s.entrance = new[] { Rt(s.classicButton), Rt(s.timeAttackButton), Rt(s.endlessButton), Rt(hint) };
        }

        static Button ModeCard(Transform parent, string name, string title, string desc, string info, Color accent,
            out Image background, out Image accentBar, out Text titleText, out Text descText)
        {
            background = Img(parent, name, UIPalette.Surface, Rounded, true);
            Item(background, 960, 340);

            accentBar = Img(background.transform, "Accent", accent, Rounded);
            var ar = Rt(accentBar);
            ar.anchorMin = new Vector2(0, 0);
            ar.anchorMax = new Vector2(0, 1);
            ar.pivot = new Vector2(0, 0.5f);
            ar.anchoredPosition = new Vector2(28, 0);
            ar.sizeDelta = new Vector2(20, -60);

            titleText = Txt(background.transform, "Title", title, 66, UIPalette.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            TopLeft(titleText, 84, -48, 700, 80);
            descText = Txt(background.transform, "Desc", desc, 38, UIPalette.TextMuted, TextAnchor.UpperLeft);
            TopLeft(descText, 84, -140, 760, 110);
            var infoText = Txt(background.transform, "Info", info, 36, accent, TextAnchor.UpperLeft, FontStyle.Bold);
            TopLeft(infoText, 84, -260, 700, 50);

            var chevron = New("Chevron", background.transform);
            Place(chevron, 1, 0.5f, -70, 0, 60, 60);
            Bar(chevron.transform, "Top", UIPalette.TextMuted, 0, 11, 36, 9, -45);
            Bar(chevron.transform, "Bottom", UIPalette.TextMuted, 0, -11, 36, 9, 45);

            var button = background.gameObject.AddComponent<Button>();
            SetupButton(button, background);
            return button;
        }

        static void TopLeft(Component c, float x, float y, float w, float h)
        {
            var rt = Rt(c);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        // ===== 04 Level select =====
        static void BuildLevelSelect(LevelSelectScreen s)
        {
            TopBar(s, "LEVELS", true, true);
            var c = Body(s, true);
            s.modeLabel = Caption(c, "ModeLabel", "Classic  |  15 seconds per run", 42);

            var grid = Group(c, "Grid", 960, 1250);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(300, 290);
            layout.spacing = new Vector2(30, 30);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            layout.childAlignment = TextAnchor.UpperCenter;

            s.tiles = new LevelTile[12];
            for (int i = 0; i < 12; i++)
            {
                var bg = Img(grid, "Level" + (i + 1).ToString("00"), UIPalette.SurfaceLight, Rounded, true);
                var tile = bg.gameObject.AddComponent<LevelTile>();
                tile.background = bg;
                tile.number = Txt(bg.transform, "Number", (i + 1).ToString(), 100, UIPalette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                Place(tile.number, 0.5f, 0.5f, 0, 25, 280, 140);
                tile.lockText = Txt(bg.transform, "Lock", "LOCKED", 30, UIPalette.LockedText, TextAnchor.MiddleCenter, FontStyle.Bold);
                Place(tile.lockText, 0.5f, 0.5f, 0, -85, 280, 50);

                var row = New("Gems", bg.transform);
                Place(row, 0.5f, 0.5f, 0, -85, 240, 60);
                tile.gemRow = row;
                tile.gems = new[]
                {
                    Gem(row.transform, "Gem1", -64, 0, 38, UIPalette.Gold),
                    Gem(row.transform, "Gem2", 0, 0, 38, UIPalette.Gold),
                    Gem(row.transform, "Gem3", 64, 0, 38, UIPalette.Gold),
                };

                tile.button = bg.gameObject.AddComponent<Button>();
                SetupButton(tile.button, bg);
                s.tiles[i] = tile;
            }

            Spacer(c, 30, 1f);
            Caption(c, "Legend", "Orange = next level    Gems = your best result", 34);
        }

        // ===== 05 Hangar (called Loadout in the code) =====
        static void BuildLoadout(LoadoutScreen s)
        {
            TopBar(s, "HANGAR", true, true);
            var c = Body(s, true);

            s.infoText = Item(Txt(c, "InfoText", "Classic  |  Level 1", 48, UIPalette.Text, TextAnchor.MiddleCenter, FontStyle.Bold), 960, 70);
            Caption(c, "Subtitle", "Tap a ship to equip it", 38);
            Spacer(c, 30, 1f);

            var cards = Group(c, "Cards", 1040, 760);
            s.cards = new ShipCard[3];
            for (int i = 0; i < 3; i++)
                s.cards[i] = ShipCardView(cards, "Ship" + (i + 1).ToString("00"), -345 + i * 345);

            Spacer(c, 30, 1f);
            s.boostText = Caption(c, "BoostText", "Extra Time boosts: 0", 36);
            s.startButton = Btn(c, "StartButton", "START RUN", ButtonStyle.Primary, 760, 180, 64);
        }

        static ShipCard ShipCardView(Transform parent, string name, float x)
        {
            var border = Bordered(parent, name, UIPalette.SurfaceLight, UIPalette.Surface, 8f, out var fill, true);
            Place(border, 0.5f, 0.5f, x, 0, 315, 720);
            var card = border.gameObject.AddComponent<ShipCard>();
            card.border = border;
            var c = border.transform;

            card.icon = Img(c, "Icon", UIPalette.Primary, Circle);
            Place(card.icon, 0.5f, 1, 0, -150, 170, 170);
            var window = Img(card.icon.transform, "Window", UIPalette.WithAlpha(UIPalette.Text, 0.85f), Circle);
            Place(window, 0.5f, 0.5f, 22, 22, 58, 58);

            card.nameText = Txt(c, "Name", "Ship", 54, UIPalette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(card.nameText, 0.5f, 1, 0, -290, 290, 70);

            card.powerBar = StatRow(c, "Power", "POWER", -370, out card.powerValue);
            card.timeBar = StatRow(c, "Time", "TIME", -470, out card.timeValue);

            card.tagBackground = Img(c, "Tag", UIPalette.SurfaceLight, Rounded);
            Place(card.tagBackground, 0.5f, 0, 0, 70, 255, 84);
            card.tagText = Txt(card.tagBackground.transform, "Label", "SELECT", 34, UIPalette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(card.tagText);

            card.button = border.gameObject.AddComponent<Button>();
            SetupButton(card.button, fill);
            card.feedback = border.GetComponent<ButtonFeedback>();
            return card;
        }

        static RectTransform StatRow(Transform parent, string name, string label, float y, out Text value)
        {
            var l = Txt(parent, name + "Label", label, 30, UIPalette.TextMuted, TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(l, 0.5f, 1, 0, y, 250, 44);
            value = Txt(parent, name + "Value", "0", 30, UIPalette.Text, TextAnchor.MiddleRight, FontStyle.Bold);
            Place(value, 0.5f, 1, 0, y, 250, 44);
            var track = Img(parent, name + "Track", UIPalette.Background, Rounded);
            Place(track, 0.5f, 1, 0, y - 46, 250, 22);
            var fill = Img(track.transform, "Fill", UIPalette.Text, Rounded);
            var fr = Rt(fill);
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = new Vector2(0.5f, 1);
            fr.offsetMin = fr.offsetMax = Vector2.zero;
            return fr;
        }

        // ===== 06 Gameplay HUD =====
        // The game itself is in Gameplay.unity and drawn by the camera, so this screen is see-through.
        // Only the top bar, timer, lives and a couple of texts are on top of it.
        static void BuildGameplay(GameplayScreen s)
        {
            var t = s.transform;
            var screenBg = s.GetComponent<Image>();
            screenBg.color = new Color(0, 0, 0, 0);
            screenBg.raycastTarget = false; // taps have to reach the game, not get eaten by the UI

            s.damageFlash = Img(t, "DamageFlash", UIPalette.WithAlpha(UIPalette.Danger, 0f), null);
            Stretch(s.damageFlash);

            // dark strip behind the HUD so the numbers are readable over the game
            var bar = Img(t, "TopBar", UIPalette.WithAlpha(UIPalette.Background, 0.8f), null);
            var br = Rt(bar);
            br.anchorMin = new Vector2(0, 1);
            br.anchorMax = new Vector2(1, 1);
            br.pivot = new Vector2(0.5f, 1);
            br.anchoredPosition = Vector2.zero;
            br.sizeDelta = new Vector2(0, TopBarHeight + 90);

            s.pauseButton = IconButton(bar.transform, "PauseButton", 0, 1, 110, -TopBarHeight / 2, false);
            SetClickSound(s.pauseButton, UISound.None); // GamePause plays the pause sound

            var score = New("Score", bar.transform);
            Place(score, 0.5f, 1, 0, -TopBarHeight / 2, 440, 160);
            var scoreLabel = Txt(score.transform, "Label", "SCORE", 30, UIPalette.TextMuted, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(scoreLabel, 0.5f, 1, 0, -22, 440, 40);
            s.scoreText = Txt(score.transform, "Value", "0", 96, UIPalette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(s.scoreText, 0.5f, 1, 0, -100, 440, 110);

            s.levelText = Txt(bar.transform, "LevelText", "LEVEL 1", 40, UIPalette.Text, TextAnchor.MiddleRight, FontStyle.Bold);
            Place(s.levelText, 1, 1, -170, -TopBarHeight / 2 + 30, 280, 60);

            // three red dots for lives, under the level text
            s.lifeIcons = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                s.lifeIcons[i] = Img(bar.transform, "Life" + (i + 1), UIPalette.Danger, Circle);
                Place(s.lifeIcons[i], 1, 1, -80 - i * 62, -TopBarHeight / 2 - 35, 46, 46);
            }

            // timer bar under the top bar
            var track = Img(bar.transform, "TimerTrack", UIPalette.Surface, Rounded);
            var tr = Rt(track);
            tr.anchorMin = new Vector2(0, 1);
            tr.anchorMax = new Vector2(1, 1);
            tr.pivot = new Vector2(0.5f, 0.5f);
            tr.offsetMin = new Vector2(50, -TopBarHeight - 55);
            tr.offsetMax = new Vector2(-190, -TopBarHeight - 25);
            s.timeFillImage = Img(track.transform, "Fill", UIPalette.Primary, Rounded);
            s.timeFill = Rt(s.timeFillImage);
            s.timeFill.anchorMin = Vector2.zero;
            s.timeFill.anchorMax = Vector2.one;
            s.timeFill.offsetMin = s.timeFill.offsetMax = Vector2.zero;
            s.timeText = Txt(bar.transform, "Time", "15s", 44, UIPalette.Text, TextAnchor.MiddleRight, FontStyle.Bold);
            Place(s.timeText, 1, 1, -95, -TopBarHeight - 40, 120, 60);

            // big text in the middle for "GO!" / "TIME UP!"
            var banner = New("Banner", t);
            Place(banner, 0.5f, 0.5f, 0, 100, 1000, 220);
            s.banner = banner.AddComponent<CanvasGroup>();
            s.banner.alpha = 0f;
            s.banner.blocksRaycasts = false;
            s.banner.interactable = false;
            s.bannerText = Txt(banner.transform, "Label", "GO!", 130, UIPalette.Primary, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(s.bannerText);
            var shadow = s.bannerText.gameObject.AddComponent<Shadow>();
            shadow.effectColor = UIPalette.WithAlpha(UIPalette.Background, 0.9f);
            shadow.effectDistance = new Vector2(5, -5);

            var hint = Txt(t, "Hint", "Tap the comets  |  Avoid the red asteroids", 34, UIPalette.WithAlpha(UIPalette.TextMuted, 0.8f));
            Place(hint, 0.5f, 0, 0, 70, 1000, 60);
        }

        // ===== 07 Pause (opens on top of gameplay) =====
        static void BuildPause(PauseScreen s)
        {
            var c = Body(s, false);
            Spacer(c, 40, 1f);

            var panel = Bordered(c, "Panel", UIPalette.SurfaceLight, UIPalette.Surface, 6f, out _, true);
            Item(panel, 820, 1080);
            s.panel = Rt(panel);
            var p = panel.transform;

            var title = Txt(p, "Title", "PAUSED", 88, UIPalette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(title, 0.5f, 1, 0, -115, 700, 110);
            s.infoText = Txt(p, "Info", "LEVEL 1  |  Score 0", 40, UIPalette.TextMuted);
            Place(s.infoText, 0.5f, 1, 0, -205, 740, 60);

            s.resumeButton = MakeButton(p, "ResumeButton", "RESUME", ButtonStyle.Primary, 0.5f, 1, 0, -350, 640, 170, 62);
            SetClickSound(s.resumeButton, UISound.None); // the resume sound plays when the game unpauses
            s.restartButton = MakeButton(p, "RestartButton", "RESTART", ButtonStyle.Secondary, 0.5f, 1, 0, -545, 640, 140, 50);
            s.settingsButton = MakeButton(p, "SettingsButton", "SETTINGS", ButtonStyle.Secondary, 0.5f, 1, 0, -710, 640, 140, 50);
            s.quitButton = MakeButton(p, "QuitButton", "QUIT TO MENU", ButtonStyle.Danger, 0.5f, 1, 0, -875, 640, 140, 50);

            Spacer(c, 40, 1f);
        }

        // ===== 08 Results =====
        static void BuildResults(ResultsScreen s)
        {
            var c = Body(s, false);
            Spacer(c, 40, 0.5f);

            s.titleText = Item(Txt(c, "Title", "LEVEL 1 CLEAR", 88, UIPalette.Primary, TextAnchor.MiddleCenter, FontStyle.Bold), 1000, 120);
            s.noteText = Caption(c, "Note", "Level 2 unlocked!", 42);

            var panel = Img(c, "Panel", UIPalette.Surface, Rounded);
            Item(panel, 900, 760);
            var p = panel.transform;

            var gemRow = New("Gems", p);
            Place(gemRow, 0.5f, 1, 0, -150, 760, 200);
            s.gems = new[]
            {
                Gem(gemRow.transform, "Gem1", -230, 0, 130, UIPalette.Gold),
                Gem(gemRow.transform, "Gem2", 0, 0, 130, UIPalette.Gold),
                Gem(gemRow.transform, "Gem3", 230, 0, 130, UIPalette.Gold),
            };

            var scoreLabel = Txt(p, "ScoreLabel", "SCORE", 36, UIPalette.TextMuted, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(scoreLabel, 0.5f, 1, 0, -320, 600, 50);
            s.scoreText = Txt(p, "ScoreValue", "0", 110, UIPalette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(s.scoreText, 0.5f, 1, 0, -415, 600, 130);

            var divider = Img(p, "Divider", UIPalette.SurfaceLight, null);
            Place(divider, 0.5f, 1, 0, -515, 700, 4);

            var coinsLabel = Txt(p, "CoinsLabel", "COINS EARNED", 36, UIPalette.TextMuted, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(coinsLabel, 0.5f, 1, 0, -575, 600, 50);
            s.coinsText = Txt(p, "CoinsValue", "+0", 76, UIPalette.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(s.coinsText, 0.5f, 1, 0, -660, 600, 100);

            Spacer(c, 40, 1f);
            s.nextButton = Btn(c, "NextButton", "NEXT LEVEL", ButtonStyle.Primary, 760, 170, 58);
            s.nextLabel = Label(s.nextButton);
            var row = Group(c, "ButtonRow", 760, 140);
            s.retryButton = MakeButton(row, "RetryButton", "RETRY", ButtonStyle.Secondary, 0.5f, 0.5f, -195, 0, 370, 140, 50);
            s.menuButton = MakeButton(row, "MenuButton", "MENU", ButtonStyle.Secondary, 0.5f, 0.5f, 195, 0, 370, 140, 50);
        }

        // ===== 09 Shop =====
        static void BuildShop(ShopScreen s)
        {
            TopBar(s, "SHOP", true, true);
            var c = Body(s, true);
            var sub = Caption(c, "Subtitle", "Spend the coins you earn from runs", 40);

            var falcon = ShopRow(c, "Item01", "Falcon", "12 points per tap\n+2 s run time", UIPalette.Secondary, null);
            falcon.kind = ShopItemKind.Ship; falcon.shipIndex = 1;
            var nova = ShopRow(c, "Item02", "Nova", "15 points per tap\n+4 s run time", UIPalette.Accent, null);
            nova.kind = ShopItemKind.Ship; nova.shipIndex = 2;
            var extra = ShopRow(c, "Item03", "Extra Time", "+5 seconds on your next run.\nYou have: 0", UIPalette.Gold, "+5s");
            extra.kind = ShopItemKind.ExtraTime;

            s.items = new[] { falcon, nova, extra };
            Spacer(c, 30, 1f);
            var note = Caption(c, "Note", "Coins are earned at the end of every run", 34);
            s.entrance = new[] { Rt(sub), Rt(falcon), Rt(nova), Rt(extra), Rt(note) };
        }

        static ShopItem ShopRow(Transform parent, string name, string title, string desc, Color iconColor, string iconText)
        {
            var row = Img(parent, name, UIPalette.Surface, Rounded);
            Item(row, 960, 280);
            var item = row.gameObject.AddComponent<ShopItem>();
            var r = row.transform;

            var icon = Img(r, "Icon", iconColor, Circle);
            Place(icon, 0, 0.5f, 125, 0, 160, 160);
            if (iconText != null)
            {
                var it = Txt(icon.transform, "Label", iconText, 46, UIPalette.OnPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
                Stretch(it);
            }
            else
            {
                var window = Img(icon.transform, "Window", UIPalette.WithAlpha(UIPalette.Text, 0.85f), Circle);
                Place(window, 0.5f, 0.5f, 20, 20, 54, 54);
            }

            item.nameText = Txt(r, "Name", title, 54, UIPalette.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            var nr = Rt(item.nameText);
            nr.anchorMin = nr.anchorMax = new Vector2(0, 0.5f);
            nr.pivot = new Vector2(0, 0.5f);
            nr.anchoredPosition = new Vector2(240, 60);
            nr.sizeDelta = new Vector2(420, 70);

            item.descText = Txt(r, "Desc", desc, 32, UIPalette.TextMuted, TextAnchor.UpperLeft);
            var dr = Rt(item.descText);
            dr.anchorMin = dr.anchorMax = new Vector2(0, 0.5f);
            dr.pivot = new Vector2(0, 1);
            dr.anchoredPosition = new Vector2(240, 18);
            dr.sizeDelta = new Vector2(440, 120);

            item.buyButton = MakeButton(r, "BuyButton", "0", ButtonStyle.Primary, 1, 0.5f, -150, 0, 240, 150, 46);
            item.buyBackground = (Image)item.buyButton.targetGraphic;
            item.priceText = Label(item.buyButton);
            return item;
        }

        // ===== 10 Settings =====
        static void BuildSettings(SettingsScreen s)
        {
            TopBar(s, "SETTINGS", true, false);
            var c = Body(s, true);

            SectionHeader(c, "AudioHeader", "AUDIO");
            s.music = ToggleRow(c, "MusicRow", "Music");
            s.sfx = ToggleRow(c, "SfxRow", "Sound Effects");

            SectionHeader(c, "VolumeHeader", "VOLUME");
            // same order as VolumeChannel (Master, Music, SFX, UI)
            string[] names = { "Master", "Music", "Effects", "UI Sounds" };
            s.volumeSliders = new Slider[names.Length];
            s.volumeValues = new Text[names.Length];
            for (int i = 0; i < names.Length; i++)
                SliderRow(c, names[i].Replace(" ", "") + "VolumeRow", names[i], out s.volumeSliders[i], out s.volumeValues[i]);

            SectionHeader(c, "GameplayHeader", "GAMEPLAY");
            s.vibration = ToggleRow(c, "VibrationRow", "Vibration");

            SectionHeader(c, "DataHeader", "DATA");
            s.resetButton = Btn(c, "ResetButton", "RESET PROGRESS", ButtonStyle.Danger, 960, 150, 46);
            s.resetLabel = Label(s.resetButton);

            Spacer(c, 40, 1f);
            Caption(c, "Footer", "Comet Run v1.1  |  Made for ITU CS-464 Lab 8", 30);
        }

        static void SectionHeader(Transform parent, string name, string text)
        {
            var h = Txt(parent, name, text, 34, UIPalette.TextMuted, TextAnchor.LowerLeft, FontStyle.Bold);
            Item(h, 940, 60);
        }

        static Image SettingsRow(Transform parent, string name, string label, bool raycast)
        {
            var row = Img(parent, name, UIPalette.Surface, Rounded, raycast);
            Item(row, 960, 150);
            var l = Txt(row.transform, "Label", label, 46, UIPalette.Text, TextAnchor.MiddleLeft);
            Stretch(l, 44, 400, 0, 0);
            return row;
        }

        static ToggleSwitch ToggleRow(Transform parent, string name, string label)
        {
            var row = SettingsRow(parent, name, label, true);
            var toggle = row.gameObject.AddComponent<ToggleSwitch>();

            toggle.stateText = Txt(row.transform, "State", "ON", 34, UIPalette.Text, TextAnchor.MiddleRight, FontStyle.Bold);
            Place(toggle.stateText, 1, 0.5f, -255, 0, 100, 50);

            toggle.track = Img(row.transform, "Track", UIPalette.Primary, Rounded);
            Place(toggle.track, 1, 0.5f, -120, 0, 150, 76);
            var knob = Img(toggle.track.transform, "Knob", UIPalette.Text, Circle);
            Place(knob, 0.5f, 0.5f, 38, 0, 62, 62);
            toggle.knob = Rt(knob);

            toggle.button = row.gameObject.AddComponent<Button>();
            SetupButton(toggle.button, row);
            Object.DestroyImmediate(row.GetComponent<ButtonFeedback>()); // the whole row shrinking on tap looked weird
            return toggle;
        }

        static void SliderRow(Transform parent, string name, string label, out Slider slider, out Text value)
        {
            var row = SettingsRow(parent, name, label, false);

            value = Txt(row.transform, "Value", "80%", 38, UIPalette.Text, TextAnchor.MiddleRight, FontStyle.Bold);
            Place(value, 1, 0.5f, -95, 0, 110, 60);

            var root = New("Slider", row.transform);
            Place(root, 0.5f, 0.5f, 90, 0, 480, 110);

            var background = Img(root.transform, "Background", UIPalette.Background, Rounded);
            var br = Rt(background);
            br.anchorMin = new Vector2(0, 0.4f);
            br.anchorMax = new Vector2(1, 0.6f);
            br.offsetMin = br.offsetMax = Vector2.zero;

            var fillArea = New("Fill Area", root.transform);
            var far = Rt(fillArea);
            far.anchorMin = new Vector2(0, 0.4f);
            far.anchorMax = new Vector2(1, 0.6f);
            far.offsetMin = new Vector2(5, 0);
            far.offsetMax = new Vector2(-5, 0);
            var fill = Img(fillArea.transform, "Fill", UIPalette.Primary, Rounded);
            Rt(fill).sizeDelta = new Vector2(10, 0);

            var handleArea = New("Handle Slide Area", root.transform);
            Stretch(handleArea, 35, 35, 0, 0);
            var handle = Img(handleArea.transform, "Handle", UIPalette.Text, Circle, true);
            Rt(handle).sizeDelta = new Vector2(70, -40);

            slider = root.AddComponent<Slider>();
            slider.fillRect = Rt(fill);
            slider.handleRect = Rt(handle);
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0.8f;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        // ===== 11 Loading (between the hangar and the game) =====
        static void BuildLoading(LoadingScreen s)
        {
            Stars(s.transform, 40, 33);
            var c = Body(s, false);
            Spacer(c, 80, 1f);

            // a planet with a little comet going around it (the comet part spins)
            var icon = Group(c, "Icon", 320, 320);
            var planet = Img(icon, "Planet", UIPalette.Surface, Circle);
            Place(planet, 0.5f, 0.5f, 0, 0, 170, 170);
            var orbit = New("Orbit", icon);
            Place(orbit, 0.5f, 0.5f, 0, 0, 320, 320);
            var head = Img(orbit.transform, "CometHead", UIPalette.Primary, Circle);
            Place(head, 0.5f, 0.5f, 0, 135, 50, 50);
            var trail1 = Img(orbit.transform, "Trail1", UIPalette.WithAlpha(UIPalette.Primary, 0.5f), Circle);
            Place(trail1, 0.5f, 0.5f, -42, 128, 34, 34);
            var trail2 = Img(orbit.transform, "Trail2", UIPalette.WithAlpha(UIPalette.Primary, 0.25f), Circle);
            Place(trail2, 0.5f, 0.5f, -78, 110, 22, 22);
            s.spinner = Rt(orbit);

            s.statusText = Item(Txt(c, "Status", "LOADING", 88, UIPalette.Text, TextAnchor.MiddleCenter, FontStyle.Bold), 1000, 120);
            s.infoText = Caption(c, "Info", "Classic  |  Level 1", 42);
            Spacer(c, 40, 0.5f);

            var bar = Group(c, "ProgressBar", 860, 56);
            var track = Img(bar, "Track", UIPalette.Surface, Rounded);
            Stretch(track);
            var fill = Img(track.transform, "Fill", UIPalette.Primary, Rounded);
            s.barFill = Rt(fill);
            s.barFill.anchorMin = Vector2.zero;
            s.barFill.anchorMax = new Vector2(0f, 1f);
            s.barFill.offsetMin = s.barFill.offsetMax = Vector2.zero;

            s.percentText = Item(Txt(c, "Percent", "0%", 64, UIPalette.Primary, TextAnchor.MiddleCenter, FontStyle.Bold), 400, 90);

            Spacer(c, 60, 1f);
            s.tipText = Caption(c, "Tip", "TIP: Tap the orange comets", 38, 110);
            Spacer(c, 60, 0.6f);
        }

        // ===== audio (mixer volumes, music player, UI sound player) =====
        // These live in the menu scene and never get unloaded.
        static void BuildAudio()
        {
            AudioAssets.SetupImportSettings();

            var root = new GameObject("Audio");
            var volumes = root.AddComponent<MixerVolumes>();
            volumes.mixer = AudioAssets.Mixer;

            var musicGo = new GameObject("Music");
            musicGo.transform.SetParent(root.transform);
            var music = musicGo.AddComponent<MusicPlayer>();
            music.sourceA = AudioAssets.AddSource(musicGo, "Music", true);
            music.sourceB = AudioAssets.AddSource(musicGo, "Music", true);
            music.menuMusic = AudioAssets.Clip("music_menu");
            music.gameMusic = AudioAssets.Clip("music_game");

            var uiGo = new GameObject("UI Sounds");
            uiGo.transform.SetParent(root.transform);
            var ui = uiGo.AddComponent<UIAudio>();
            ui.source = AudioAssets.AddSource(uiGo, "UI");
            ui.hover = AudioAssets.Clip("ui_hover");
            ui.click = AudioAssets.Clip("ui_click");
            ui.back = AudioAssets.Clip("ui_back");
            ui.panelOpen = AudioAssets.Clip("ui_panel_open");
            ui.panelClose = AudioAssets.Clip("ui_panel_close");
            ui.pause = AudioAssets.Clip("ui_pause");
            ui.resume = AudioAssets.Clip("ui_resume");
            ui.success = AudioAssets.Clip("ui_success");
            ui.error = AudioAssets.Clip("ui_error");
            ui.gem = AudioAssets.Clip("ui_gem");
        }

        static void SetClickSound(Button button, UISound sound)
        {
            var feedback = button.GetComponent<ButtonFeedback>();
            if (feedback != null) feedback.clickSound = sound;
        }

        // ===== toast message at the bottom =====
        static Toast BuildToast(Transform parent)
        {
            var bg = Img(parent, "Toast", UIPalette.SurfaceLight, Rounded);
            Place(bg, 0.5f, 0, 0, 200, 900, 110);
            var group = bg.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0;
            group.blocksRaycasts = false;
            group.interactable = false;
            var label = Txt(bg.transform, "Label", "", 38, UIPalette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(label, 30, 30, 0, 0);

            var toast = bg.gameObject.AddComponent<Toast>();
            toast.group = group;
            toast.background = bg;
            toast.label = label;
            return toast;
        }
    }
}
