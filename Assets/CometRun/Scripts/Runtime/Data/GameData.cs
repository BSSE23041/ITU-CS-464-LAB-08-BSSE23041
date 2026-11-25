using System;
using CometRun.UI;
using UnityEngine;

namespace CometRun.Data
{
    public enum GameMode { Classic, TimeAttack, Endless }

    public class ShipInfo
    {
        public string name;
        public int price;
        public int points;       // how many points you get per comet
        public int bonusSeconds; // extra seconds added to the run
        public Color color;
    }

    public struct RunResult
    {
        public GameMode mode;
        public int level;
        public int score;
        public int gems;
        public int coins;
        public bool cleared;
        public bool died;      // lost all lives before the timer ran out
        public bool unlockedNext;
        public bool hasNext;
        public int nextGemScore;
    }

    // All the player stuff lives here: coins, unlocked levels, gems, ships and settings.
    // It's static so every screen can just read it. Everything gets saved in PlayerPrefs.
    public static class GameData
    {
        public const int LevelCount = 12;
        public const int EndlessUnlockLevel = 6;
        public const int ExtraTimePrice = 100;
        public const int ExtraTimeSeconds = 5;
        const int StartCoins = 300;

        public static readonly ShipInfo[] Ships =
        {
            new ShipInfo { name = "Comet", price = 0, points = 10, bonusSeconds = 0, color = UIPalette.Primary },
            new ShipInfo { name = "Falcon", price = 250, points = 12, bonusSeconds = 2, color = UIPalette.Secondary },
            new ShipInfo { name = "Nova", price = 500, points = 15, bonusSeconds = 4, color = UIPalette.Accent },
        };

        public static int Coins { get; private set; }
        public static int UnlockedLevels = 1;
        public static int[] Gems = new int[LevelCount];
        public static bool[] ShipOwned = { true, false, false };
        public static int SelectedShip;
        public static int ExtraTimeCharges;

        public static GameMode Mode = GameMode.Classic;
        public static int SelectedLevel;

        public static bool Music = true;
        public static bool Sfx = true;
        public static bool Vibration = true;

        public static event Action CoinsChanged;

        public static bool EndlessUnlocked => UnlockedLevels > EndlessUnlockLevel || Gems[EndlessUnlockLevel - 1] > 0;

        public static int RunSeconds
        {
            get
            {
                switch (Mode)
                {
                    case GameMode.TimeAttack: return 10;
                    case GameMode.Endless: return 20;
                    default: return 15;
                }
            }
        }

        public static string ModeName(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.TimeAttack: return "Time Attack";
                case GameMode.Endless: return "Endless";
                default: return "Classic";
            }
        }

        public static int LevelsCleared()
        {
            int count = 0;
            foreach (int g in Gems) if (g > 0) count++;
            return count;
        }

        // --- coins ---
        // both of these fire CoinsChanged so the coin pill in the top bar updates itself

        public static void AddCoins(int amount)
        {
            Coins += amount;
            Save();
            CoinsChanged?.Invoke();
        }

        public static bool TrySpend(int amount)
        {
            if (Coins < amount) return false;
            Coins -= amount;
            Save();
            CoinsChanged?.Invoke();
            return true;
        }

        // --- scoring ---

        // score you need for 1, 2 or 3 gems. Gets a bit harder each level
        // and depends on the mode because the runs are different lengths.
        public static int ScoreForGem(int gem, int level)
        {
            float baseScore = RunSeconds * 8f * (1f + Mathf.Max(0, level) * 0.05f);
            return Mathf.RoundToInt(baseScore * (0.5f + gem * 0.5f));
        }

        // works out gems + coins for a finished run, saves the best gems
        // and unlocks the next level if you just cleared the newest one.
        // If you died you still get coins for your score, but no gems (the level isn't cleared).
        public static RunResult RecordResult(int score, bool died = false)
        {
            var r = new RunResult { mode = Mode, level = SelectedLevel, score = score, died = died };
            if (!died)
            {
                for (int g = 1; g <= 3; g++)
                    if (score >= ScoreForGem(g, SelectedLevel)) r.gems = g;
            }

            r.cleared = r.gems > 0;
            r.coins = score / 5 + r.gems * 10;
            r.nextGemScore = ScoreForGem(Mathf.Min(r.gems + 1, 3), SelectedLevel);

            if (Mode != GameMode.Endless && SelectedLevel >= 0)
            {
                if (r.gems > Gems[SelectedLevel]) Gems[SelectedLevel] = r.gems;
                if (r.cleared && SelectedLevel == UnlockedLevels - 1 && UnlockedLevels < LevelCount)
                {
                    UnlockedLevels++;
                    r.unlockedNext = true;
                }
                r.hasNext = r.cleared && SelectedLevel + 1 < UnlockedLevels;
            }

            AddCoins(r.coins);
            return r;
        }

        // --- save / load ---
        // arrays are saved as comma separated strings since PlayerPrefs can't store arrays

        public static void Load()
        {
            Coins = PlayerPrefs.GetInt("coins", StartCoins);
            UnlockedLevels = Mathf.Clamp(PlayerPrefs.GetInt("unlocked", 1), 1, LevelCount);
            ReadInts(PlayerPrefs.GetString("gems", ""), Gems);
            var owned = new int[Ships.Length];
            ReadInts(PlayerPrefs.GetString("ships", "1,0,0"), owned);
            for (int i = 0; i < Ships.Length; i++) ShipOwned[i] = owned[i] == 1;
            ShipOwned[0] = true; // first ship is free so you always have one
            SelectedShip = Mathf.Clamp(PlayerPrefs.GetInt("ship", 0), 0, Ships.Length - 1);
            if (!ShipOwned[SelectedShip]) SelectedShip = 0;
            ExtraTimeCharges = PlayerPrefs.GetInt("extraTime", 0);
            Music = PlayerPrefs.GetInt("music", 1) == 1;
            Sfx = PlayerPrefs.GetInt("sfx", 1) == 1;
            Vibration = PlayerPrefs.GetInt("vibration", 1) == 1;
            // (the volume sliders save themselves now, see MixerVolumes)
        }

        public static void Save()
        {
            PlayerPrefs.SetInt("coins", Coins);
            PlayerPrefs.SetInt("unlocked", UnlockedLevels);
            PlayerPrefs.SetString("gems", string.Join(",", Gems));
            var owned = new string[Ships.Length];
            for (int i = 0; i < Ships.Length; i++) owned[i] = ShipOwned[i] ? "1" : "0";
            PlayerPrefs.SetString("ships", string.Join(",", owned));
            PlayerPrefs.SetInt("ship", SelectedShip);
            PlayerPrefs.SetInt("extraTime", ExtraTimeCharges);
            PlayerPrefs.SetInt("music", Music ? 1 : 0);
            PlayerPrefs.SetInt("sfx", Sfx ? 1 : 0);
            PlayerPrefs.SetInt("vibration", Vibration ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ResetProgress()
        {
            Coins = StartCoins;
            UnlockedLevels = 1;
            Gems = new int[LevelCount];
            ShipOwned = new[] { true, false, false };
            SelectedShip = 0;
            ExtraTimeCharges = 0;
            Save();
            CoinsChanged?.Invoke();
        }

        // turns "3,2,0,..." back into the int array (missing/bad values just become 0)
        static void ReadInts(string csv, int[] into)
        {
            for (int i = 0; i < into.Length; i++) into[i] = 0;
            if (string.IsNullOrEmpty(csv)) return;
            var parts = csv.Split(',');
            for (int i = 0; i < parts.Length && i < into.Length; i++)
                int.TryParse(parts[i], out into[i]);
        }
    }
}
