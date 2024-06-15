using MemoryToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace BetterMaps.Maui.Sample
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

#if DEBUG
            // Configure logging
            builder.Logging.AddDebug();
            // Ensure UseLeakDetection is called after logging has been configured!
            builder.UseLeakDetection();
#endif

            return builder.Build();
        }
    }
}