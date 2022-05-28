using System;

namespace Maui.Controls.BetterMaps
{
    internal static class ColorExtensions
    {
        internal static float ToAndroidHue(this Color color)
            => color.GetHue() * 360f % 360f;
    }
}