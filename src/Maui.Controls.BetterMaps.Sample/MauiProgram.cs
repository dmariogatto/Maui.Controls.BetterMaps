using Android.Gms.Maps;
using Maui.Controls.BetterMaps.Android;

namespace Maui.Controls.BetterMaps.Sample
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
#if ANDROID
                .UseMauiMaps(lightThemeAsset: "map.style.light.json", darkThemeAsset: "map.style.dark.json")
#elif IOS || MACCATALYST
                .UseMauiMaps()
#endif
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            return builder.Build();
        }
    }
}