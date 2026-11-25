using UnityEngine;

namespace CometRun.UI
{
    // All the colours are here so every screen uses the same ones.
    // Dark navy for the background and orange for buttons, they're opposite on the colour wheel.
    public static class UIPalette
    {
        public static readonly Color Background = Hex(0x0F172A);   // deep navy
        public static readonly Color Surface = Hex(0x1E293B);      // cards / panels
        public static readonly Color SurfaceLight = Hex(0x334155); // raised items, borders
        public static readonly Color Primary = Hex(0xF59E0B);      // main action orange
        public static readonly Color OnPrimary = Hex(0x0F172A);    // text on orange
        public static readonly Color Secondary = Hex(0x14B8A6);    // teal, secondary actions
        public static readonly Color Accent = Hex(0xA78BFA);       // violet, third mode / ship
        public static readonly Color Text = Hex(0xF8FAFC);         // main text
        public static readonly Color TextMuted = Hex(0x94A3B8);    // captions, hints
        public static readonly Color Danger = Hex(0xF87171);       // warnings on dark bg
        public static readonly Color DangerDark = Hex(0xB91C1C);   // warning toast fill
        public static readonly Color Gold = Hex(0xFACC15);         // coins and gems
        public static readonly Color GemEmpty = Hex(0x0F172A);     // empty gem slot
        public static readonly Color Disabled = Hex(0x475569);     // off / unavailable
        public static readonly Color Locked = Hex(0x111A2E);       // locked tiles
        public static readonly Color LockedText = Hex(0x64748B);   // text on locked tiles

        public static Color Hex(int rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
        }

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
