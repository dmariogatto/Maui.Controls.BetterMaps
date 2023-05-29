using Maui.Controls.BetterMaps.Handlers;
using Microsoft.Maui.LifecycleEvents;

namespace Maui.Controls.BetterMaps
{
    public static partial class AppHostBuilderExtensions
    {
        public static MauiAppBuilder UseMauiMaps(this MauiAppBuilder builder)
        {
#if ANDROID
            builder.Services.AddSingleton<IGeocoder, Android.GeocoderBackend>();
#elif IOS
            builder.Services.AddSingleton<IGeocoder, iOS.GeocoderBackend>();
#endif

            builder
                .ConfigureMauiHandlers(handlers =>
                {
                    handlers.AddMauiMaps();
                })
                .ConfigureLifecycleEvents(events =>
                {
#if ANDROID
                    // Log everything in this one
                    events.AddAndroid(android => android
                        .OnCreate((a, b) =>
                        {
                            MauiBetterMaps.Init(a, b);
                        }));
#elif IOS
                    events.AddiOS(ios =>
                    {
                        MauiBetterMaps.Init();
                    });
#endif
                });

            return builder;
        }

        public static IMauiHandlersCollection AddMauiMaps(this IMauiHandlersCollection handlersCollection)
        {
#if ANDROID || IOS
            handlersCollection.AddHandler<Map, MapHandler>();
            handlersCollection.AddHandler<Pin, MapPinHandler>();
            handlersCollection.AddHandler<MapElement, MapElementHandler>();
#endif
            return handlersCollection;
        }
    }
}