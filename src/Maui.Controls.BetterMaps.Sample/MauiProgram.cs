using Maui.Controls.BetterMaps.Handlers;

namespace Maui.Controls.BetterMaps.Sample
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureMauiHandlers(handlers =>
                {
                    handlers.AddHandler(typeof(Map), typeof(MapHandler));
                    handlers.AddHandler(typeof(Pin), typeof(MapPinHandler));
                    handlers.AddHandler(typeof(Polyline), typeof(MapElementHandler));
                    handlers.AddHandler(typeof(Polygon), typeof(MapElementHandler));
                    handlers.AddHandler(typeof(Circle), typeof(MapElementHandler));
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            return builder.Build();
        }
    }
}