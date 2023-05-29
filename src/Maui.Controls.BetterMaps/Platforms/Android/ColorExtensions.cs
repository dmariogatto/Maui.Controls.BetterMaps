namespace Maui.Controls.BetterMaps.Android
{
    internal static class ColorExtensions
    {
        internal static float ToAndroidHue(this Color color)
            => color.GetHue() * 360f % 360f;
    }
}