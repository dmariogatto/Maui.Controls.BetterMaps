using Android.App;
using Android.Gms.Common;
using Android.Gms.Maps;
using Android.OS;
using BetterMaps.Maui;
using BetterMaps.Maui.Android;
using BetterMaps.Maui.Handlers;

namespace BetterMaps
{
    public static class ActivityExtensions
    {
        private static bool _initialized;

        internal static readonly Dictionary<MapTheme, string> MapThemeAssetNames = new Dictionary<MapTheme, string>();

        public static void GoogleMapsSdkInit(this Activity activity, Bundle bundle)
            => GoogleMapsSdkInit(activity, bundle, GoogleMapsRenderer.Latest, null, null, null);

        public static void GoogleMapsSdkInit(
            this Activity activity,
            Bundle bundle,
            GoogleMapsRenderer renderer,
            Action<MapsInitializer.Renderer> onGoogleMapsSdkInitialized,
            string lightThemeAsset,
            string darkThemeAsset)
        {
            if (_initialized)
                return;

            _initialized = true;

            MapHandler.Bundle = bundle;

            if (!string.IsNullOrWhiteSpace(lightThemeAsset))
                MapThemeAssetNames[MapTheme.Light] = lightThemeAsset;
            if (!string.IsNullOrWhiteSpace(darkThemeAsset))
                MapThemeAssetNames[MapTheme.Dark] = darkThemeAsset;

#pragma warning disable 618
            if (GooglePlayServicesUtil.IsGooglePlayServicesAvailable(activity) == ConnectionResult.Success)
#pragma warning restore 618
            {
                try
                {
                    var rendererCallback = default(OnMapsSdkInitializedCallback);
                    if (onGoogleMapsSdkInitialized is not null)
                    {
                        void onMapsSdkInitialized(object sender, OnGoogleMapsSdkInitializedEventArgs args)
                        {
                            onGoogleMapsSdkInitialized?.Invoke(args.Renderer);

                            if (rendererCallback is not null)
                            {
                                rendererCallback.OnGoogleMapsSdkInitialized -= onMapsSdkInitialized;
                                rendererCallback.Dispose();
                                rendererCallback = null;
                            }
                        }

                        rendererCallback = new OnMapsSdkInitializedCallback();
                        rendererCallback.OnGoogleMapsSdkInitialized += onMapsSdkInitialized;
                    }

                    _ = renderer switch
                    {
                        GoogleMapsRenderer.Legacy => MapsInitializer.Initialize(activity, MapsInitializer.Renderer.Legacy, rendererCallback),
                        _ => MapsInitializer.Initialize(activity, MapsInitializer.Renderer.Latest, rendererCallback),
                    };
                }
                catch (Exception e)
                {
                    Console.WriteLine("Google Play Services Not Found");
                    Console.WriteLine("Exception: {0}", e);
                }
            }
        }
    }
}
