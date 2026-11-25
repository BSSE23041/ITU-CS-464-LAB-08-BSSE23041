using CometRun.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CometRun.EditorTools
{
    public enum ButtonStyle { Primary, Secondary, Danger, Ghost }

    // Small helpers for making UI objects from code. The scene builder uses these
    // a lot so every button/text has the same font, colours and settings.
    public static class UIFactory
    {
        public static Font Font;
        public static Sprite Rounded;
        public static Sprite Circle;

        // only using Unity's built in font and sprites, no imported assets
        public static void LoadResources()
        {
            Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        }

        public static GameObject New(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // 5 is the UI layer
            go.transform.SetParent(parent, false);
            return go;
        }

        public static RectTransform Rt(Component c) => (RectTransform)c.transform;
        public static RectTransform Rt(GameObject go) => (RectTransform)go.transform;

        // anchors to one point on the parent, then sets position and size
        public static RectTransform Place(RectTransform rt, float ax, float ay, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform Place(Component c, float ax, float ay, float x, float y, float w, float h)
            => Place(Rt(c), ax, ay, x, y, w, h);

        public static RectTransform Place(GameObject go, float ax, float ay, float x, float y, float w, float h)
            => Place(Rt(go), ax, ay, x, y, w, h);

        // fills the whole parent, padding is optional
        public static RectTransform Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform Stretch(Component c, float left = 0, float right = 0, float top = 0, float bottom = 0)
            => Stretch(Rt(c), left, right, top, bottom);

        public static RectTransform Stretch(GameObject go, float left = 0, float right = 0, float top = 0, float bottom = 0)
            => Stretch(Rt(go), left, right, top, bottom);

        public static Image Img(Transform parent, string name, Color color, Sprite sprite, bool raycast = false)
        {
            var go = New(name, parent);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sprite == Rounded ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Text Txt(Transform parent, string name, string text, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var go = New(name, parent);
            var t = go.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.05f;
            t.supportRichText = true;
            t.raycastTarget = false;
            return t;
        }

        // fake border: one image behind and a slightly smaller one on top for the fill
        public static Image Bordered(Transform parent, string name, Color border, Color fill, float thickness, out Image fillImage, bool raycast = false)
        {
            var outer = Img(parent, name, border, Rounded, raycast);
            fillImage = Img(outer.transform, "Fill", fill, Rounded, raycast);
            Stretch(fillImage, thickness, thickness, thickness, thickness);
            return outer;
        }

        // the 4 button styles I use everywhere (primary / secondary / danger / plain text)
        public static Button MakeButton(Transform parent, string name, string label, ButtonStyle style,
            float ax, float ay, float x, float y, float w, float h, int fontSize = 52)
        {
            Image target;
            Image root;
            Color textColor;

            switch (style)
            {
                case ButtonStyle.Primary:
                    root = Img(parent, name, UIPalette.Primary, Rounded, true);
                    target = root;
                    textColor = UIPalette.OnPrimary;
                    break;
                case ButtonStyle.Secondary:
                    root = Bordered(parent, name, UIPalette.Secondary, UIPalette.Surface, 6f, out target, true);
                    textColor = UIPalette.Text;
                    break;
                case ButtonStyle.Danger:
                    root = Bordered(parent, name, UIPalette.Danger, UIPalette.Surface, 6f, out target, true);
                    textColor = UIPalette.Danger;
                    break;
                default:
                    root = Img(parent, name, new Color(0, 0, 0, 0), null, true);
                    target = root;
                    textColor = UIPalette.TextMuted;
                    break;
            }

            Place(root, ax, ay, x, y, w, h);
            var text = Txt(root.transform, "Label", label, fontSize, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(text, 10, 10, 0, 0);

            var button = root.gameObject.AddComponent<Button>();
            SetupButton(button, target);
            return button;
        }

        // same tint + press animation on every button. Navigation is off because
        // it's a touch game and keyboard selection was leaving buttons highlighted.
        public static void SetupButton(Button button, Graphic target)
        {
            button.targetGraphic = target;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.6f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            if (button.GetComponent<ButtonFeedback>() == null) button.gameObject.AddComponent<ButtonFeedback>();
        }

        public static Text Label(Button b) => b.transform.Find("Label").GetComponent<Text>();

        // thin rotated bar, I use it for the arrows, the pause icon and dividers
        public static Image Bar(Transform parent, string name, Color color, float x, float y, float w, float h, float angle)
        {
            var img = Img(parent, name, color, Rounded);
            Place(img, 0.5f, 0.5f, x, y, w, h);
            img.transform.localRotation = Quaternion.Euler(0, 0, angle);
            return img;
        }

        // a gem is just a square turned 45 degrees
        public static Image Gem(Transform parent, string name, float x, float y, float size, Color color)
        {
            var img = Img(parent, name, color, Rounded);
            Place(img, 0.5f, 0.5f, x, y, size, size);
            img.transform.localRotation = Quaternion.Euler(0, 0, 45);
            return img;
        }

        // random faint dots for stars. Fixed seed so they're in the same spot every rebuild
        public static void Stars(Transform parent, int count, int seed)
        {
            var root = New("Stars", parent);
            Stretch(root);
            var rnd = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float size = 6 + rnd.Next(0, 3) * 4;
                var dot = Img(root.transform, "Star", UIPalette.WithAlpha(UIPalette.Text, 0.12f + (float)rnd.NextDouble() * 0.2f), Circle);
                Place(dot, (float)rnd.NextDouble(), (float)rnd.NextDouble(), 0, 0, size, size);
            }
        }
    }
}
