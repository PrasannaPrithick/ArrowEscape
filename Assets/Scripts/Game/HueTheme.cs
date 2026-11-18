using System;
using System.Globalization;
using UnityEngine;

namespace HueGates.Game
{
    public static class HueTheme
    {
        public static readonly Color Background = Hex("14172B");
        public static readonly Color Panel = Hex("1E2342");
        public static readonly Color Ink = Hex("F4F6FF");
        public static readonly Color Muted = Hex("A7B0D6");
        public static readonly Color Mint = Hex("3DDC97");
        public static readonly Color Coral = Hex("FF6B6B");
        public static readonly Color Sun = Hex("FFC94D");
        public static readonly Color Sky = Hex("4DA3FF");
        public static readonly Color Violet = Hex("9B6BFF");
        public static readonly Color Navy = Hex("14172B");

        public static Color Palette(string name)
        {
            switch (name)
            {
                case "coral": return Coral;
                case "sun": return Sun;
                case "mint": return Mint;
                case "sky": return Sky;
                case "violet": return Violet;
                default: return Color.white;
            }
        }

        public static Color Hex(string html)
        {
            if (html.StartsWith("#")) html = html.Substring(1);
            byte r = byte.Parse(html.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            byte g = byte.Parse(html.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            byte b = byte.Parse(html.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return new Color32(r, g, b, 255);
        }
    }
}
