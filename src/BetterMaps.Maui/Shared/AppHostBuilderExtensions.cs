using BetterMaps.Maui.Handlers;
using Microsoft.Maui.LifecycleEvents;

namespace BetterMaps.Maui
{
    public static partial class AppHostBuilderExtensions
    {
#if ANDROID
        public static MauiAppBuilder UseMauiMaps(
            this MauiAppBuilder builder,
            Android.GoogleMapsRenderer renderer = Android.GoogleMapsRenderer.Latest,
            Action<global::Android.Gms.Maps.MapsInitializer.Renderer> onGoogleMapsSdkInitialized = null,
            string lightThemeAsset = null,
            string darkThemeAsset = null)
#elif IOS || MACCATALYST
        public static MauiAppBuilder UseMauiMaps(this MauiAppBuilder builder)
#endif
        {
#if ANDROID
            builder.Services.AddSingleton<IGeocoder, Android.GeocoderBackend>();
#elif IOS || MACCATALYST
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
                    events.AddAndroid(android => android
                        .OnCreate((a, b) =>
                        {
                            a.GoogleMapsSdkInit(renderer, onGoogleMapsSdkInitialized, lightThemeAsset, darkThemeAsset);
                        }));
#endif
                });

            return builder;
        }

        public static IMauiHandlersCollection AddMauiMaps(this IMauiHandlersCollection handlersCollection)
        {
#if ANDROID || IOS || MACCATALYST
            handlersCollection.AddHandler<Map, MapHandler>();
            handlersCollection.AddHandler<Pin, MapPinHandler>();
            handlersCollection.AddHandler<MapElement, MapElementHandler>();
#endif
            return handlersCollection;
        }
    }
}